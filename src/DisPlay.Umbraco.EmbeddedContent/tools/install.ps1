# Install.ps1
param($installPath, $toolsPath, $package, $project)

$xml = New-Object xml

# find the Web.config file
$configFolder = $project.ProjectItems.Item("Config")

if ($configFolder) {
    $config = $configFolder.ProjectItems.Item("ClientDependency.config")

    if ($config) {
        # find its path on the file system
        $localPath = $config.Properties.Item("LocalPath")

        # load Web.config as XML
        $xml.Load($localPath.Value)

        # select the node
        $node = $xml.SelectSingleNode("clientDependency")

        # change the connectionString value
        $node.SetAttribute("version", [math]::abs((Get-Date).ToUniversalTime().GetHashCode()))

        # save the Web.config file
        $xml.Save($localPath.Value)
    }
}