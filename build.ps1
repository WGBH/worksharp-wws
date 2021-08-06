#!/usr/bin/env pwsh

param (
    [Parameter(Mandatory = $true)]
    [string]$WwsVersion,
    [switch]$SkipComponentIfAlreadyPacked,
    [int]$WarningLevel = 4,
    [string]$PushTo
)

dotnet tool restore

$ProgressPreference = 'SilentlyContinue'

$root = "WorkSharp.Wws"

Set-Location $PSScriptRoot/Endpoints

$endpoints = Get-Content endpoints.json | ConvertFrom-Json

if(Test-Path build) {
    Remove-Item -Force -Recurse build/*
} else {
    New-Item -ItemType Directory build > $null
}

$template = @"
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns="http://www.microsoft.com/xml/schema/linq">
  <Namespaces>
    <Namespace Schema="urn:com.workday/bsvc" Clr="$root.{0}" />
  </Namespaces>
</Configuration>
"@

foreach($endpoint in $endpoints) {
    $package = "$root.Endpoints.$endpoint"

    if((Test-Path "$PSScriptRoot/out/$package.$WwsVersion*") -and $SkipComponentIfAlreadyPacked) {
        "Skipping $endpoint..."
        continue
    }

    Set-Location $PSScriptRoot/Endpoints/build
    New-Item -ItemType Directory $endpoint > $null
    Set-Location $endpoint
    Copy-Item $PSScriptRoot/Endpoints/template.csproj "$package.csproj"

    "Generating package for $endpoint version $WwsVersion..."
    $template -f $endpoint > "$endpoint.xsd.config"

    Invoke-WebRequest "https://community.workday.com/sites/default/files/file-hosting/productionapi/$endpoint/v$WwsVersion/$endpoint.xsd" `
        -OutFile "$endpoint.xsd"
    if(!$?) { exit 1 }
    dotnet LinqToXsd gen "$endpoint.xsd" `
        --Config "$endpoint.xsd.config"
    if(!$?) { exit 1 }

    Invoke-WebRequest "https://community.workday.com/sites/default/files/file-hosting/productionapi/$endpoint/v$WwsVersion/$endpoint.wsdl" `
        -OutFile "$endpoint.wsdl"
    if(!$?) { exit 1 }
    dotnet run -c Release --project $PSScriptRoot/ClientBuilder -- "$endpoint.wsdl"
    if(!$?) { exit 1 }

    dotnet pack -c Release -p:Version=$WwsVersion -p:WarningLevel=$WarningLevel -o $PSScriptRoot/out
    if(!$?) { exit 1 }

    if ($PushTo -ne '') {
        dotnet nuget push "$PSScriptRoot/out/$package.$WwsVersion*" -s $PushTo
        if(!$?) { exit 1 }
    }
}

Set-Location $PSScriptRoot