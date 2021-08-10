#!/usr/bin/env pwsh

# Copyright 2021 WGBH Educational Foundation
# Licensed under the Apache License, Version 2.0

param (
    [Parameter(Mandatory = $true)]
    [string]$WwsVersion,
    [Parameter(Mandatory = $true)]
    [int]$PackagePatch,
    [switch]$SkipComponentIfAlreadyPushed,
    [int]$WarningLevel = 4,
    [string]$PushTo,
    [string]$ApiKey
)

$ProgressPreference = 'SilentlyContinue'

if ($WwsVersion -notmatch '^\d+.\d+$') {
    Write-Error 'WwsVersion is not of the correct format!'
    exit 1
}

if ($PackagePatch -lt 0 -or $PackagePatch -gt 100) {
    Write-Error 'PackagePatch is out of range!'
    exit 1
}

$packageVersion = "$WwsVersion.$PackagePatch"
$root = 'WorkSharp.Wws'

$template = @"
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns="http://www.microsoft.com/xml/schema/linq">
  <Namespaces>
    <Namespace Schema="urn:com.workday/bsvc" Clr="$root.{0}" />
  </Namespaces>
</Configuration>
"@

if($PushTo -ne '' -and $SkipComponentIfAlreadyPushed) {
    $dotnetNugetOutputLines = dotnet nuget list source
    $indexInOuput = 1 + $dotnetNugetOutputLines.IndexOf(
        $dotnetNugetOutputLines.Where({ $_ -like "* $PushTo *" }))
    $serviceIndexUri = $dotnetNugetOutputLines[$indexInOuput].Trim()

    $packageBaseUri = (Irm $serviceIndexUri).resources.Where(
        {$_.'@type' -eq 'PackageBaseAddress/3.0.0'} ).'@id'
    if(!$?) { exit 1 }
}

dotnet tool restore

Set-Location $PSScriptRoot/Endpoints

$endpoints = Get-Content endpoints.json | ConvertFrom-Json

if(Test-Path build) {
    Remove-Item -Force -Recurse build/*
} else {
    New-Item -ItemType Directory build > $null
}

foreach($endpoint in $endpoints) {
    $packageName = "$root.Endpoints.$endpoint"

    if($PushTo -ne '' -and $SkipComponentIfAlreadyPushed) {
        $packageLower = $packageName.ToLower()

        $packageUri = "$packageBaseUri$packageLower/$packageVersion/$packageLower.nuspec"
        Invoke-RestMethod -Method HEAD $packageUri `
            -SkipHttpErrorCheck -StatusCodeVariable packageLookupStatus > $null
        if(!$?) { exit 1 }

        if ($packageLookupStatus -eq 200) {
            "Skipping $endpoint version $WwsVersion..."
            continue
        }
    }

    $baseEndpointInfoUri = 'https://community.workday.com/sites/default/files/file-hosting/productionapi/' `
        + "$endpoint/v$WwsVersion/$endpoint"
    Invoke-RestMethod -Method HEAD "$baseEndpointInfoUri.xsd" `
        -SkipHttpErrorCheck -StatusCodeVariable xsdLookupStatus > $null
    if ($xsdLookupStatus -eq 404) {
        Write-Warning "Endpoint $endpoint does not exist for WWS version $WwsVersion."
        continue
    }

    "Generating package for $endpoint version $WwsVersion..."
    Set-Location $PSScriptRoot/Endpoints/build
    New-Item -ItemType Directory $endpoint > $null
    Set-Location $endpoint
    Copy-Item $PSScriptRoot/Endpoints/template.csproj "$packageName.csproj"

    $template -f $endpoint > "$endpoint.xsd.config"

    Invoke-WebRequest "$baseEndpointInfoUri.xsd" -OutFile "$endpoint.xsd"
    if(!$?) { exit 1 }
    dotnet LinqToXsd gen "$endpoint.xsd" --Config "$endpoint.xsd.config"
    if(!$?) { exit 1 }

    Invoke-WebRequest "$baseEndpointInfoUri.wsdl" -OutFile "$endpoint.wsdl"
    if(!$?) { exit 1 }
    dotnet run -c Release --project $PSScriptRoot/ClientBuilder -- "$endpoint.wsdl"
    if(!$?) { exit 1 }

    dotnet pack -c Release -p:Version="$packageVersion" -p:Endpoint=$endpoint `
        -p:WarningLevel=$WarningLevel -o $PSScriptRoot/out
    if(!$?) { exit 1 }

    if ($PushTo -ne '') {
        $package = "$PSScriptRoot/out/$packageName.$packageVersion.nupkg"

        if ($ApiKey -ne '') {
            $pushOutput = dotnet nuget push $package `
                --source $PushTo --api-key $ApiKey --skip-duplicate | Join-String -Separator "`n"
        } else {
            $pushOutput = dotnet nuget push $package `
                --source $PushTo --skip-duplicate | Join-String -Separator "`n"
        }

        $pushResult = $?

        if($pushOutput -like "*$package*already exists*" ) {
            Write-Warning $pushOutput
        } else {
            $pushOutput
        }

        if (!$pushResult) { exit 1 }
    }
}

Set-Location $PSScriptRoot