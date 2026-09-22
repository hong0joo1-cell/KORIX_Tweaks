using System.Management;

namespace KORIX_Tweaks;

public static class RestoreManager
{
	public static void CreateRestorePoint()
	{
		ManagementClass managementClass = new ManagementClass("SystemRestore");
		ManagementBaseObject methodParameters = managementClass.GetMethodParameters("CreateRestorePoint");
		methodParameters["Description"] = "KORIX Tweaks Restore Point";
		methodParameters["RestorePointType"] = 0;
		methodParameters["EventType"] = 100;
		managementClass.InvokeMethod("CreateRestorePoint", methodParameters, null);
	}
}
