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
   
   Create-Sources-Nuspec
   
   & $nuget_exe pack "$base_dir/src/WebApi.Formatting.JsonMask/WebApi.Formatting.JsonMask.Sources.nuspec" -OutputDirectory $build_artifacts_dir -Verbosity detailed
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

function Create-Sources-Nuspec
{
   function Create-DependElement([xml]$xml, [string]$id, [string]$version) {
      $xmlNS = $nuspecXml.DocumentElement.NamespaceURI

      $node = $xml.CreateElement("dependency", $xmlNS)
      $node.SetAttribute("id", $id)
      $node.SetAttribute("version", $version)
      
      return $node
   }

   function Create-FileElement([xml]$xml, [string]$src, [string]$target) {
      $xmlNS = $nuspecXml.DocumentElement.NamespaceURI

      $node = $xml.CreateElement("file", $xmlNS)
      $node.SetAttribute("src", $src)
      $node.SetAttribute("target", $target)
      
      return $node
   }
   
   $nupkg = @(get-childitem $build_artifacts_dir\*.nupkg)[0]
   
   [String]$nuspec = "WebApi.Formatting.JsonMask.nuspec"
   [String]$dest = $env:TEMP
   [String]$extractedFile = "$dest/$nuspec"
   [String]$sourcesNuspec = "$base_dir/src/WebApi.Formatting.JsonMask/WebApi.Formatting.JsonMask.Sources.nuspec"
   
   [Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") > $null
   $zipStream = [System.IO.Compression.ZipFile]::OpenRead($nupkg.FullName)
   $fileStream = new-object System.IO.FileStream($extractedFile), 'OpenOrCreate', 'Write', 'Read'
   
   foreach ($zippedFile in $zipStream.Entries) {
      if ($zippedFile.Name -eq $nuspec) {
         $file = $zippedFile.Open()
         $file.CopyTo($fileStream)
         $file.Close()
      }
   }
   $fileStream.Close()
   $zipStream.Dispose()
   
   [xml]$nuspecXml = Get-Content $extractedFile
   $xmlNS = $nuspecXml.DocumentElement.NamespaceURI
   
   $nuspecXml.package.metadata.id = $nuspecXml.package.metadata.id + ".Sources"
   
   $nuspecXml.package.metadata.dependencies.AppendChild((Create-DependElement $nuspecXml "TaskHelpers.Sources" "0.3")) > $null
   
   $filesNode = $nuspecXml.CreateElement("files", $xmlNS)
   $filesNode.AppendChild((Create-FileElement $nuspecXml "*.cs" "content\App_Packages\WebApi.Formatters.JsonMask")) > $null
   $filesNode.AppendChild((Create-FileElement $nuspecXml "..\Package-Examples\FormatterConfig-Example.cs.pp" "content\App_Start")) > $null
   $nuspecXml.package.AppendChild($filesNode) > $null
   
   $nuspecXml.Save($sourcesNuspec)
}

