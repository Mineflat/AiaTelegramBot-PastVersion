#!/bin/bash
# Принимаемые параметры:
# $1 - строка подключения к базе данных (в формате postgresql://postgres@192.168.1.200:5432),
# Где:
# postgresql://postgres@192.168.1.200:5432
#              [имя_БД]@[Адрес СУБД]:[порт СУБД]
# $2 - пароль для подключения к базе данных в чистом виде

function CheckIntegrity {
    SOURCES_LIST=('../ServerSettings.sh' './CoreFunctions.sh')
    MISSED_SOURCES=""
    for source in ${allThSOURCES_LISTreads[@]}; do
        if [[ ! -f source ]]; then
            MISSED_SOURCES="$MISSED_SOURCES\n\t$source"
        fi
    done
    if [[ $MISSED_SOURCES != "" ]]; then
            echo -e "\t\e[1;31m[$(date)][CRITICAL]: Не удалось обнаружить файлы:\n$MISSED_SOURCES"
            exit 1
    fi
    unset SOURCES_LIST
}

function CheckPG_Connection {
    su - postgres -c "PGPASSWORD='$2' psql $1 -c '\l'"
    if [[ "$?" != 0 ]]; then
        echo -e "\t\e[1;31m[$(date)][CRITICAL]: Не удалось подключиться к базе данных. Проверьте настройки СУБД"
        exit 1
    fi
}

CheckIntegrity
CheckPG_Connection $1 $2 
