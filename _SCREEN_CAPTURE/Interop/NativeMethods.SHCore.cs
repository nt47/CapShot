using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace _SCREEN_CAPTURE.Interop
{
	public partial class NativeMethods
    {
		[System.Runtime.InteropServices.DllImport(DllNames.SHCore)]
		public static extern bool SetProcessDPIAware();
		// 导入SetProcessDpiAwareness函数
		[DllImport(DllNames.SHCore, SetLastError = true)]
		public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

		// 定义DPI感知枚举
		public enum PROCESS_DPI_AWARENESS
		{
			ProcessDpiUnaware = 0,
			ProcessSystemDpiAware = 1,
			ProcessPerMonitorDpiAware = 2
		}
	}
}
