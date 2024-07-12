#!/bin/bash
function VerifyDB_Connection {
    exit 1
}
# Функция, которая проверяет, что токен бота:
# 1. Валиден со стороны телеграмм-серверов
# 2. Его контрольная сумма не существует в базе данных (бот не запущен в реестре)
function VerifyToken_Connection {
    exit 1
}
# Функция логирования
# Принимает параметры:
# $1 - уровень логирования (string)
# $2 - сообщение (string)
# Данная функциоя является уязвимой, т.к. параметры $1 и $2
# не проверяется на наличие исполняемого кода или произвольной подстановки
# и гипотетически могут быть исполнены  
function LOG {
    if [ -d "$LOG_PATH" ]; then exit 0; fi
    trimmed="$(echo $2 | xargs)"
    echo "[$(date '+%Y/%m/%d %H:%M:%S')][$1]: $trimmed"
    if [[ "$?" != "0" ]]; then unset LOG_PATH && exit 1; fi
    exit 0
}

function ListBots {
    ls /var/lib/lxc/*
}