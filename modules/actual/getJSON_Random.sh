#!/bin/bash
# $1 - Полный путь к JSON-файлу 
# $2 - Корневой элемент JSON-массива
# $3 - Имя интересующего элемента JSON-массива (наименование поля-идентификатора)
# $4 - Интересующий идентификатор JSON-массива (значение поля-идентификатора)
# $5 - Наименование элемента по указанному идентификатору (значение поля N по указанному идентификатору)
### Пример:
# Содержание JSON-файла по пути /opt/books.json:
#   {
#       "books":[
#           {"id":"1", "name":"Let Us C", "author":"Yashavant Kanetkar"},
#           {"id":"2", "name":"Rich Dad Poor Dad", "author":"Robert Kiyosaki "},
#           {"id":"3", "name":"Introduction to Algorithms", "author":"Cormen"},
#       ]
#   }
### Интересующая строка: содержимое поля author у кники с id = 2
### Значения параметров:
# $1 - /opt/books.json
# $2 - books
# $3 - id
# $4 - 2
# $5 - author
### В скрипте: 
# ./getJSON_Random.sh /opt/books.json books id 2 author
jq ".$2[] | select(.$3==\"$4\") | .$5" $1