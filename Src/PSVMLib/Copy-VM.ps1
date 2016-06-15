<# 
.SYNOPSIS
	Clone VM
.DESCRIPTION
	Creates a clone of vm.
.PARAMETER Path
	Path to create new virtual machine copy.
.PARAMETER Link
	Set this switch to create a linked clone instead of a full clone.
.EXAMPLE
	Clone vm to target folder.
	$vm | Copy-VM -Path c:\MyVms\NewVM.vmx -Linked
.INPUTS
	IVirtualMachine
#> 
[CmdletBinding]
function Copy-VM
{
	param([Parameter(Mandatory = $true, ValueFromPipeline = $true)]$VM, [switch]$Linked, [Parameter(Mandatory = $true)][string]$Path)

	Process 
	{

	}
}