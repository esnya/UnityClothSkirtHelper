#!/bin/sh

VERSION=$1
for src in $(ls dist/*.unitypackage)
do
  dst=$(echo $src | sed -re "s/(-.+?)?\.unitypackage/-v$VERSION.unitypackage/g")
  mv dist/$src $dst
done
