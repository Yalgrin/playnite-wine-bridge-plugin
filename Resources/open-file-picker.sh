#!/bin/bash
find_dialog_app() {
  if command -v kdialog >/dev/null 2>&1; then
    echo "kdialog"
  elif command -v zenity >/dev/null 2>&1; then
    echo "zenity"
  elif command -v yad >/dev/null 2>&1; then
    echo "yad"
  else
    return 1
  fi
}

process_filters() {
  PICKER=$1
  FILTER=$2
  if [ "$PICKER" = "kdialog" ]; then
    local in="$FILTER"
    if [[ -z "${in}" ]]; then
      printf "All files (*)"
      return 0;
    fi

    # Split on the first '|'
    local name="${in%%|*}"
    local patterns="${in#*|}"

    # Convert ; -> spaces, remove trailing '*'s from each token (e.g. *.jpg* -> *.jpg)
    local out_patterns
    out_patterns="$(
      printf '%s' "$patterns" \
        | tr ';' ' ' \
        | sed -E 's/\*+(\s|$)/\1/g; s/[[:space:]]+/ /g; s/^ //; s/ $//'
    )"

    printf '%s (%s)|All files (*)' "$name" "$out_patterns"
  elif [ "$PICKER" = "zenity" ] || [ "$PICKER" = "yad" ]; then
    local input="$FILTER"
    if [[ -z "${input}" ]]; then
      printf ""
      return 0;
    fi

    # Split on the first '|'
    local left="${input%%|*}"
    local right="${input#*|}"

    # Turn ';' into spaces, and remove stray '*' characters (keeping the leading "*." pattern)
    right="${right//;/ }"
    right="$(printf '%s' "$right" | sed -E 's/(\.[[:alnum:]]+)\*+/\1/g; s/[[:space:]]+/ /g; s/^ //; s/ $//')"

    # Add spaces around the pipe and normalize spacing
    printf '%s | %s' "$left" "$right"
  fi
}

pick_file() {
  PICKER=$1
  MODE=$2
  FILTER=$3
  WORKDIR=$4
  if [ "$PICKER" = "kdialog" ]; then
    CMD_PARAMS=()
    if [ "$MODE" = "file" ]; then
      CMD_PARAMS+=( --getopenfilename )
    elif [ "$MODE" = "file-multiple" ]; then
      CMD_PARAMS+=( --getopenfilename --multiple )
    elif [ "$MODE" = "directory" ]; then
      CMD_PARAMS+=( --getexistingdirectory )
    elif [ "$MODE" = "save" ]; then
      CMD_PARAMS+=( --getsavefilename )
    fi
    CMD_PARAMS+=( --separate-output )
    kdialog "${CMD_PARAMS[@]}" "$WORKDIR" "$(process_filters "$PICKER" "$FILTER")"
  elif [ "$PICKER" = "zenity" ]; then
    CMD_PARAMS=()
    if [ "$MODE" = "file" ]; then
      CMD_PARAMS+=( --file-selection )
    elif [ "$MODE" = "file-multiple" ]; then
      CMD_PARAMS+=( --file-selection --multiple )
    elif [ "$MODE" = "directory" ]; then
      CMD_PARAMS+=( --file-selection --directory )
    elif [ "$MODE" = "save" ]; then
      CMD_PARAMS+=( --file-selection --save )
    fi
    if [ "$MODE" != "directory" ]; then
      SPECIFIC_FILTER="$(process_filters "$PICKER" "$FILTER")"
      if [[ -n "${SPECIFIC_FILTER}" ]]; then
        CMD_PARAMS+=( --file-filter "$SPECIFIC_FILTER" )
      fi
    fi
    CMD_PARAMS+=( --file-filter "All files | *" )
    CMD_PARAMS+=( --separator=$'\n' )
    CMD_PARAMS+=( --filename="$WORKDIR" )

    zenity "${CMD_PARAMS[@]}"
  elif [ "$PICKER" = "yad" ]; then
    CMD_PARAMS=()
    if [ "$MODE" = "file" ]; then
      CMD_PARAMS+=( --file )
    elif [ "$MODE" = "file-multiple" ]; then
      CMD_PARAMS+=( --file --multiple )
    elif [ "$MODE" = "directory" ]; then
      CMD_PARAMS+=( --file --directory )
    elif [ "$MODE" = "save" ]; then
      CMD_PARAMS+=( --file --save )
    fi
    if [ "$MODE" != "directory" ]; then
      SPECIFIC_FILTER="$(process_filters "$PICKER" "$FILTER")"
      if [[ -n "${SPECIFIC_FILTER}" ]]; then
        CMD_PARAMS+=( --file-filter "$SPECIFIC_FILTER" )
      fi
    fi
    CMD_PARAMS+=( --file-filter "All files | *" )
    CMD_PARAMS+=( --separator=$'\n' )
    CMD_PARAMS+=( --workdir="$WORKDIR" )
    yad "${CMD_PARAMS[@]}"
#     --width=1200 --height=800
  else
    echo "No GUI picker found (kdialog/zenity/yad)" >&2
    return 1
  fi
}

SELECTED_PICKER=$1
SELECTED_MODE=$2
SELECTED_FILTER=$3
SELECTED_WORKDIR=$4

if [ "$SELECTED_PICKER" = "auto" ]; then
  SELECTED_PICKER="$(find_dialog_app)"
fi

file="$(pick_file "$SELECTED_PICKER" "$SELECTED_MODE" "$SELECTED_FILTER" "$SELECTED_WORKDIR")" || exit 1
echo "$file"
