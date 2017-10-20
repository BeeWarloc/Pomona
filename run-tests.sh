#!/bin/bash

cd "$(dirname "$0")"

PREDICATE="cat != TODO"
CONFIG=Debug
MSBUILD=msbuild
NUGET=nuget
ARG_PREFIX=
NUNIT_EXTRA_ARGS=

for arg in "$@"; do
    case $arg in
    --one-worker)
        NUNIT_EXTRA_ARGS="$NUNIT_EXTRA_ARGS --workers=1"
        ;;
    --debug)
        CONFIG=Debug
        ;;
    --release)
        CONFIG=Release
        ;;
    --build)
        ALWAYS_BUILD=1
        ;;
    -h|--help)
        echo "Available options:"
        echo "    --release           Uses release configuration"
        echo "    --debug             Uses debug configuration (default)"
        echo "    --one-worker        Disable parallel test running"
        echo "    --build             Build before running tests, even if assemblies already exists"
        echo "                        If assemblies don't exist they will be built anyway."
        exit 0
        ;;
    *)
        echo "ERROR: Invalid argument '$arg'" >&2
        exit 1
        ;;
    esac
done

case "$(uname -s)" in
   CYGWIN*|MINGW*|MSYS*)
     DOTNET=""
     if ! which "$MSBUILD"; then
        vswhere=packages/vswhere/tools/vswhere.exe
        if [ ! -f $vswhere ]; then
            "$NUGET" install vswhere -ExcludeVersion -Output packages
        fi
        # Command line flags starting with / have to be prefixed with another / in MSYS
        # If we don't do that they will be seen as a path and converted to Windows path
        ARG_PREFIX=/
        MSBUILD="$(cygpath -u "`$vswhere -requires Microsoft.Component.MSBuild -property installationPath`"/MSBuild/*/bin/MSBuild.exe)"
     fi
     ;;
   *)
     DOTNET=mono
     PREDICATE="$PREDICATE && cat != WindowsRequired"
     ;;
esac

runner=./packages/NUnit.ConsoleRunner.3.7.0/tools/nunit3-console.exe

if [ ! -f "$runner" ]; then
    "$NUGET" restore Pomona.sln
fi

assemblies=""

for i in Pomona.UnitTests Pomona.SystemTests Pomona.IndependentClientTests Pomona.SystemTests.ClientCompatibility;
do
    assembly=tests/$i/bin/$CONFIG/$i.dll
    if [ ! -f "$assembly" ] || [ "$ALWAYS_BUILD" = 1 ]; then
        "$MSBUILD" Pomona.sln $ARG_PREFIX/p:Configuration=$CONFIG
    fi
    assemblies="$assemblies $assembly"
done

$DOTNET $runner $assemblies --result TestResult.$CONFIG.xml --where "$PREDICATE" --labels=All $NUNIT_EXTRA_ARGS
