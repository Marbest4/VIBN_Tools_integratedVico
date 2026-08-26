[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Read-SecretValue {
    param([Parameter(Mandatory)][string]$Prompt)

    $secureValue = Read-Host -Prompt $Prompt -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

Write-Host 'VIBN Tools – lokale Ersteinrichtung'
Write-Host 'Leere Eingabe lässt einen bereits vorhandenen Wert unverändert.'

$apiKey = Read-SecretValue 'Businessmap/Kanbanize API-Key'
if (-not [string]::IsNullOrWhiteSpace($apiKey)) {
    [Environment]::SetEnvironmentVariable('VIBN_VICO_KANBANIZE_API_KEY', $apiKey, 'User')
    Write-Host "Kanbanize API-Key wurde für $env:USERNAME gesetzt (Länge: $($apiKey.Length))."
}

$rdpPassword = Read-SecretValue 'Gemeinsames RDP-Kennwort'
if (-not [string]::IsNullOrWhiteSpace($rdpPassword)) {
    [Environment]::SetEnvironmentVariable('VIBN_RDP_PASSWORD', $rdpPassword, 'User')
    Write-Host "RDP-Kennwort wurde für $env:USERNAME gesetzt (Länge: $($rdpPassword.Length))."
}

$apiKey = $null
$rdpPassword = $null
Write-Host 'Einrichtung abgeschlossen. VIBN Tools und gegebenenfalls Visual Studio vollständig neu starten.'
Read-Host 'Zum Schließen Eingabetaste drücken'
