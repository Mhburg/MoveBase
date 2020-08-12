param ([string]$filePath)
[xml]$xdoc  = get-content $filePath

# Use dll reference instead of project reference
$reference = $xdoc.Project.ItemGroup.Reference[0].Clone()
$reference.SetAttribute("Include", "NotooShabby.RimWorldUtility")
$reference.Private = "False"
$xdoc.Project.ItemGroup.AppendChild($reference)

# Remove project reference
$nsmgr = New-Object -TypeName System.Xml.XmlNamespaceManager -ArgumentList $xdoc.NameTable
$nsmgr.AddNamespace("ns", $xdoc.DocumentElement.NamespaceURI)
$delete = $xdoc.DocumentElement.SelectSingleNode('/ns:Project/ns:ItemGroup/ns:ProjectReference[ns:Name="RimWorldUtility"]', $nsmgr)
$delete.ParentNode.RemoveChild($delete)

# Remove generation of documentation file
$delete = $xdoc.DocumentElement.SelectNodes('/ns:Project/ns:PropertyGroup/ns:DocumentationFile', $nsmgr)
Foreach ($doc in $delete)
{
    $doc.ParentNode.RemoveChild($doc)
}

# Remove PostBuild event used by MSVS
$delete = $xdoc.DocumentElement.SelectSingleNode('/ns:Project/ns:PropertyGroup/ns:PostBuildEvent', $nsmgr)
$delete.ParentNode.RemoveChild($delete)

$xdoc.Save($filePath)
