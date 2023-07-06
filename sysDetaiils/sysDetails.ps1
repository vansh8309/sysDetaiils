$scriptPath = Join-Path $PSScriptRoot "sysDetails.csv"

    #$ping = Test-Connection -ComputerName 10.10.11.212 -Count 1 -Quiet
        $macs = Get-NetAdapter | Select-Object -Property Name, MacAddress
        $printedHostname = $false
        $creationDate = Get-Date
        $printedOSname = $false
        $printedOSver = $false
        $hostname =  $env:COMPUTERNAME
        $osInfo = Get-WmiObject -Class Win32_OperatingSystem
        $osName = $osInfo.Caption
        $osVersion = $osInfo.Version
        $users = Get-WinEvent -FilterHashtable @{logname='Security';ID=4624} |
            Where-Object { $_.Properties[9].Value -like 'user32 ' } |
            Select-Object -Unique @{
                Name = "UserName"
                Expression = { $_.Properties[6].Value + "\" + $_.Properties[5].Value }
            }, @{
                Name = "LogOnTime"
                Expression = { $_.TIMECREATED }
            } -First 50
        $uusers = $users | Select-Object -Unique -Property UserName
        $localusrname = @()
        $localactive = @()
        $localtype = @()
        $locusers = net user | Select-Object -Skip 4 |
            Where-Object { $_ -ne "The command completed successfully." } |
            ForEach-Object { $_.Split(" ") | Where-Object { $_ -ne "" } } 
        foreach ($locusr in $locusers) {
            $a = net user $locusr
            $localusrname += $a[0].Split(" ") | Select-Object -Last 1
            $localactive += $a[5].Split(" ") | Select-Object -Last 1
            $localtype += $a[22].replace("Local Group Memberships      ", "")
        }
        $usbs = Get-ItemProperty -Path HKLM:\SYSTEM\CurrentControlSet\Enum\USBSTOR\*\* | Select-Object -Unique FriendlyName
        $FriendlyName = @()
        $HardwareID = @()
        $cusbs = @()
        if ($null -eq $usbs) {
            $FriendlyName = @("NA")
            $HardwareID = @("NA")
        }
        else {
            foreach ($div in $usbs) {
                $FriendlyName += $div.FriendlyName
                $cusb = Get-CimInstance Win32_PnPEntity | Where-Object { $_.Name -eq $div.FriendlyName } | Select-Object -ExpandProperty DeviceID
                if ($cusb -eq $null) {
                    $cusbs += ""
                } else {
                    $cusbs += $cusb
                }
            }
        }
        $apps = @()
        $paths = @(
            'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall',
            'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'
        )
        foreach ($path in $paths) {
            $apps += Get-ChildItem -Path $path |
                Get-ItemProperty |
                Select-Object @{
                    Name = "Installed Programs"
                    Expression = { $_.DisplayName }
                }, @{
                    Name = "Installed Programs Version"
                    Expression = { $_.DisplayVersion }
                }
        }
        $apps = $apps | Sort-Object -Unique -Property "Installed Programs"
        $res = for ($i = 0; $i -lt $apps.Count; $i++) {
            $ip = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -like $macs.Name[$i] }
            $hostnameOutput = if (-not $printedHostname) { $hostname; $printedHostname = $true }
            $creationDateOutput = if (-not $creationDateName) { $creationDate; $creationDateName = $true }
            $osNameOutput = if (-not $printedOSname) { $osName; $printedOSname = $true }
            $osVersionOutput = if (-not $printedOSver) { $osVersion; $printedOSver = $true }
            $uusersOutput = if ($uusers.Count -gt 1) { $uusers.UserName[$i] } else { $uusers.UserName }
            
            [PSCustomObject]@{
                'Hostname' = $hostnameOutput
                'Creation Date' = $creationDateOutput
                'OS Name' = $osNameOutput
                'OS Version' = $osVersionOutputs
                'Interface Name' = $macs.Name[$i]
                'MAC Address' = $macs.MacAddress[$i]
                'IP Address' = $ip.IpAddress
                'Local Username' = $localusrname[$i]
                'Is Active' = $localactive[$i]
                'Local User Group' = $localtype[$i]
                'USB Device Name' = $FriendlyName[$i]
                'USB Hardware ID' = $cusbs[$i]
                'Last Loggedon Users' = $users.UserName[$i]
                'Logon Date and Time' = $users.LogOnTime[$i]
                'Unique Loggedon Users' = $uusersOutput
                'Installed Programs' = $apps.'Installed Programs'[$i]
                'Installed Programs Version' = $apps.'Installed Programs Version'[$i]
            }
        }
        
        $res | Export-Csv -Path $scriptPath -NoTypeInformation