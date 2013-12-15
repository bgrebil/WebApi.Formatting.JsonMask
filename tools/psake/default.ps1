Properties {
        $base_dir = resolve-path .\..\..\
        $packages_dir = "$base_dir\packages"
        $build_artifacts_dir = "$base_dir\build"
        $solution_name = "$base_dir\WebApi.Formatting.JsonMask.sln"
        $test_project = "$base_dir\Build.proj"
        $test_runner = "$packages_dir\xunit.1.9.2\lib\net20"
        $xunit_runner = "$base_dir\tools\xunit.runners\tools"
        $xunit_build_destination = "$build_artifacts_dir\tools\xunit"
        $xunitConsole = "$xunit_build_destination\xunit.console.exe"
        $nuget_exe = "$base_dir\.nuget\Nuget.exe"
}

Task Default -Depends BuildFormatter, RunUnitTests, NuGetBuild

Task BuildFormatter -Depends Clean, Build

Task Clean {
   function Delete([String]$Path) { del "$Path" -Force -Recurse -ErrorAction SilentlyContinue }
   
   Delete "$build_artifacts_dir"
   Exec { msbuild $solution_name /v:Quiet /t:Clean /p:Configuration=Release }
}

Task Build -depends Clean {
   Exec { msbuild $solution_name /v:Quiet /t:Build /p:Configuration=Release /p:OutDir=$build_artifacts_dir\ } 
}

Task RunUnitTests -depends Build {
   Exec { msbuild $test_project /t:Tests }
   if ($LastExitCode -gt 0) {
      Exit-Build "$LastExitCode unit tests failed"
   }
   if ($LastExitCode -lt 0) {
      Exit-Build "Unit test was terminated by a fatal error"
   }
}

Task NuGetBuild -depends Build {
   & $nuget_exe pack "$base_dir/src/WebApi.Formatting.JsonMask/WebApi.Formatting.JsonMask.csproj" -Build -OutputDirectory $build_artifacts_dir -Verbosity detailed -Properties Configuration=Release
}

# Utility Functions
#######################
 
# Exits the script when an error occurs and prints a message to the user
function Exit-Build
{
   [CmdletBinding()]
   param(
      [Parameter(Position = 0, Mandatory = $true)][String]$Message
   )
 
   Write-Host $("`nExiting build because task [{0}] failed.`n->`t$Message.`n" -f $psake.context.Peek().currentTaskName) -ForegroundColor Red
 
   Exit
}
