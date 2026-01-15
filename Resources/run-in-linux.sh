#!/bin/bash
set -x

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

print_log "Arguments: $*"
print_log "Going to run command: $COMMAND"
print_log "Correlation id: $CORRELATION_ID"
print_log "Async tracking: $ASYNC_TRACKING"
print_log "Tracking expression: $TRACKING_EXPRESSION"
print_log "Tracking directory: $TRACKING_DIRECTORY"

if [ "$ASYNC_TRACKING" = "1" ];
then
  PREVIOUS_PROCESS_IDS=$(pgrep -f "$TRACKING_EXPRESSION")
  
  print_log "Running async command..."
  eval $COMMAND
  print_log "Done execution, looking for processes..."
  
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
  
  print_log "Found process IDs: $ASYNC_PROCESS_IDS"
  
  for PID in $ASYNC_PROCESS_IDS
  do
    print_log "Found process ID: $PID with command line: $(ps -p $PID -o cmd)"
  done
  
  for PID in $ASYNC_PROCESS_IDS
  do
    print_log "Waiting for process $PID to complete..."
    tail --pid=$PID -f /dev/null
    print_log "Process $PID completed"
  done
  
  print_log "Done tracking"
  
  rm $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID
else
  touch $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID
  touch $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID-ready
  print_log "Running synchronous command..."
  eval $COMMAND
  print_log "Done execution"
  rm $TRACKING_DIRECTORY/wine-bridge-$CORRELATION_ID
fi 

echo '-----------------------------'
