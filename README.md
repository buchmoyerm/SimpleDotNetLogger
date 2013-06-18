This logger is written using c# .net version 3.5

This repo only contains the c# [visual studio 2008] project file for the logger.

When built it should create a .dll that can be referenced and the logger can be used.

example c# use:

```c#
namespace MyApp
{
	class Program
	{
		public static int main (string[] args)
		{
			FileLog.Logger MyLogger = new FileLog.Logger("MyFile", "LogDir");
			MyLogger.Log("Test log line");
		}
	}
}
```