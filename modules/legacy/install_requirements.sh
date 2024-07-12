#!/bin/bash
if [[ "$(id | grep root)" == ""  ]]; then
    echo -e "\e[1;31m[$(date '+%Y/%m/%d %H:%M:%S')][CRITICAL]\e[0m Для установки дополнительного ПО необходимы права ROOT"
    exit 1
fi
apt update >> ./install_requirements.log
apt install apt install lxc lxc-templates cgroup-bin bridge-utils debootstrap -y