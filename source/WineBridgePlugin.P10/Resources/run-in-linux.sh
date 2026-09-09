#!/bin/bash

while IFS= read -r name; do
  case "$name" in
    WINE*|PROTON*|DXVK*|LUTRIS*|UMU*|STEAM*|__GL_SHADER_DISK_CACHE*) unset "$name" ;;
  esac
done < <(compgen -e)
unset "GAME_NAME"
unset "OS"
unset "WINE"
unset "LD_PRELOAD"

COMMAND=$1
COMMAND=$(echo "$COMMAND" | base64 -d)
CORRELATION_ID=$2
TRACKING_MODE=$3
TRACKING_EXPRESSION=$4
TRACKING_EXPRESSION=$(echo "$TRACKING_EXPRESSION" | base64 -d)
TRACKING_DIRECTORY=$5
TRACKING_DIRECTORY=$(echo "$TRACKING_DIRECTORY" | base64 -d)

print_log(){
  information=$1
  echo "$(printf '%(%F %T)T')" ${1} | tee -a $TRACKING_DIRECTORY/wine-bridge.log
}

await_for_ready() {
  for i in {0..239}
  do
    CURRENT_PROCESS_IDS=$(pgrep -f "$TRACKING_EXPRESSION")
    ASYNC_PROCESS_IDS=$(comm -23 <(echo "$CURRENT_PROCESS_IDS" | sort) <(echo "$PREVIOUS_PROCESS_IDS" | sort))
    
    if [[ -n "$ASYNC_PROCESS_IDS" ]]; then
      touch $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID
      touch $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-ready
      break
    fi
    sleep 1
  done
}

track_processes() {
  if [[ -z "$ASYNC_PROCESS_IDS" ]]; then
    print_log "$CORRELATION_ID - No process found!"
    return
  fi
  
  for PID in $ASYNC_PROCESS_IDS
  do
    print_log "$CORRELATION_ID - Found process ID: $PID with command line: $(ps -p $PID -o cmd)"
  done
  
  for PID in $ASYNC_PROCESS_IDS
  do
    print_log "$CORRELATION_ID - Waiting for process $PID to complete..."
    tail --pid=$PID -f /dev/null
    print_log "$CORRELATION_ID - Process $PID completed"
  done
}

track_processes_continuously() {
  if [[ -z "$ASYNC_PROCESS_IDS" ]]; then
    print_log "No process found!"
    return
  fi
  
  while [[ -n "$ASYNC_PROCESS_IDS" ]];
  do
    for PID in $ASYNC_PROCESS_IDS
    do
      print_log "$CORRELATION_ID - Found process ID: $PID with command line: $(ps -p $PID -o cmd)"
    done
    
    for PID in $ASYNC_PROCESS_IDS
    do
      print_log "$CORRELATION_ID - Waiting for process $PID to complete..."
      tail --pid=$PID -f /dev/null
      print_log "$CORRELATION_ID - Process $PID completed"
    done
    
    await_for_more_processes
  done
}

await_for_more_processes() {
  print_log "$CORRELATION_ID - Awaiting more processes..."
    
  for i in {0..10}
  do
    CURRENT_PROCESS_IDS=$(pgrep -f "$TRACKING_EXPRESSION")
    ASYNC_PROCESS_IDS=$(comm -23 <(echo "$CURRENT_PROCESS_IDS" | sort) <(echo "$PREVIOUS_PROCESS_IDS" | sort))
    
    if [[ -n "$ASYNC_PROCESS_IDS" ]]; then
      print_log "$CORRELATION_ID - Found additional process IDs: $ASYNC_PROCESS_IDS"
      break
    fi
    sleep 0.2
  done
  
  if [[ -z "$ASYNC_PROCESS_IDS" ]]; then
    print_log "$CORRELATION_ID - No additional process found!"
  fi
}

echo '-----------------------------'

print_log "$CORRELATION_ID - Arguments: $*"
print_log "$CORRELATION_ID - Going to run command: $COMMAND"
print_log "$CORRELATION_ID - Tracking mode: \"$TRACKING_MODE\""
print_log "$CORRELATION_ID - Tracking expression: \"$TRACKING_EXPRESSION\""
print_log "$CORRELATION_ID - Tracking directory: \"$TRACKING_DIRECTORY\""

if [ "$TRACKING_MODE" = "Asynchronous" ];
then
  PREVIOUS_PROCESS_IDS=$(pgrep -f "$TRACKING_EXPRESSION")
  
  mkfifo $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-input
  
  print_log "$CORRELATION_ID - Running async command..."
  eval "($COMMAND) & disown" > >(tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-output) 2> >(tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-error >&2) &
  COMMAND_PID=$!
  echo "$COMMAND_PID" | tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-pid
  print_log "$CORRELATION_ID - Started process with PID $COMMAND_PID. Waiting for it to end..."
  wait "$COMMAND_PID"
  COMMAND_EXIT_STATUS=$?
  print_log "$CORRELATION_ID - Done execution (status: $COMMAND_EXIT_STATUS), looking for processes..."
  echo "$COMMAND_EXIT_STATUS" | tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-status
  
  await_for_ready
  
  print_log "$CORRELATION_ID - Found process IDs: $ASYNC_PROCESS_IDS"
  
  track_processes
  
  print_log "$CORRELATION_ID - Done tracking"
  
  rm $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID
elif [ "$TRACKING_MODE" = "AsynchronousContinuous" ];
then
  PREVIOUS_PROCESS_IDS=$(pgrep -f "$TRACKING_EXPRESSION")
  
  mkfifo $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-input
  
  print_log "$CORRELATION_ID - Running async continuous command..."
  eval "($COMMAND) & disown" > >(tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-output) 2> >(tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-error >&2) &
  COMMAND_PID=$!
  echo "$COMMAND_PID" | tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-pid
  print_log "$CORRELATION_ID - Started process with PID $COMMAND_PID. Waiting for it to end..."
  wait "$COMMAND_PID"
  COMMAND_EXIT_STATUS=$?
  print_log "$CORRELATION_ID - Done execution (status: $COMMAND_EXIT_STATUS), looking for processes..."
  echo "$COMMAND_EXIT_STATUS" | tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-status
  
  await_for_ready
  
  print_log "$CORRELATION_ID - Found process IDs: $ASYNC_PROCESS_IDS"
  
  track_processes_continuously
  
  print_log "$CORRELATION_ID - Done tracking continuously"
  
  rm $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID
else
  touch $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID
  touch $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-ready
  mkfifo $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-input
  
  print_log "$CORRELATION_ID - Running synchronous command..."
  eval "$COMMAND" > >(tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-output) 2> >(tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-error >&2) &
  COMMAND_PID=$!
  echo "$COMMAND_PID" | tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-pid
  print_log "$CORRELATION_ID - Started process with PID $COMMAND_PID. Waiting for it to end..."
  wait "$COMMAND_PID"
  COMMAND_EXIT_STATUS=$?
  print_log "$CORRELATION_ID - Done execution (status: $COMMAND_EXIT_STATUS)"
  
  echo "$COMMAND_EXIT_STATUS" | tee -a $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-status
  rm $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID
fi 

echo '-----------------------------'
