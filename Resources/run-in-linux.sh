#!/bin/bash
COMMAND=$1
COMMAND=$(echo "$COMMAND" | base64 -d)
CORRELATION_ID=$2
ASYNC_TRACKING=$3
TRACKING_EXPRESSION=$4
TRACKING_EXPRESSION=$(echo "$TRACKING_EXPRESSION" | base64 -d)
TRACKING_DIRECTORY=$5
TRACKING_DIRECTORY=$(echo "$TRACKING_DIRECTORY" | base64 -d)

print_log(){
  information=$1
  echo "$(printf '%(%F %T)T')" ${1} | tee -a $TRACKING_DIRECTORY/wine-bridge.log
}

echo '-----------------------------'

print_log "$CORRELATION_ID - Arguments: $*"
print_log "$CORRELATION_ID - Going to run command: $COMMAND"
print_log "$CORRELATION_ID - Async tracking: $ASYNC_TRACKING"
print_log "$CORRELATION_ID - Tracking expression: $TRACKING_EXPRESSION"
print_log "$CORRELATION_ID - Tracking directory: $TRACKING_DIRECTORY"

if [ "$ASYNC_TRACKING" = "1" ];
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
  
  for i in {0..119}
  do
    CURRENT_PROCESS_IDS=$(pgrep -f "$TRACKING_EXPRESSION")
    ASYNC_PROCESS_IDS=$(comm -23 <(echo "$CURRENT_PROCESS_IDS" | sort) <(echo "$PREVIOUS_PROCESS_IDS" | sort))
    
    if [[ -n "$ASYNC_PROCESS_IDS" ]]; then
      touch $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID
      touch $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-ready
      break
    fi
    sleep 2
  done
  
  print_log "$CORRELATION_ID - Found process IDs: $ASYNC_PROCESS_IDS"
  
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
  
  print_log "$CORRELATION_ID - Done tracking"
  
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
