$folderName = "NetworkReports"               # Name of the shared folder
$folderPath = "C:\$folderName"               # Path to the shared folder
$shareName = $folderName                     # Name of the share

# Check if the folder already exists
if (Test-Path -Path $folderPath -PathType Container) {
    Write-Output "Success: The folder '$folderName' already exists."
}
else {
    # Create the folder
    $folder = New-Item -Path $folderPath -ItemType Directory -ErrorAction SilentlyContinue
    if ($folder) {
        # Create a new share with Everyone permissions
        $share = Get-SmbShare | Where-Object { $_.Name -eq $shareName }
        if ($null -eq $share) {
            $shareParams = @{
                Name = $shareName
                Path = $folderPath
                Description = $shareName
                FullAccess = "Everyone"
            }
            $result = New-SmbShare @shareParams -ErrorAction SilentlyContinue
            if ($result -eq $null) {
                Write-Output "Success: Shared folder '$folderName' created successfully."
            }
            else {
                Write-Output "Failure: Failed to create shared folder '$folderName'."
            }
        }
        else {
            Write-Output "Success: Shared folder '$folderName' created successfully."
        }
    }
    else {
        Write-Output "Success: The shared folder '$folderName' already exists."
    }
}