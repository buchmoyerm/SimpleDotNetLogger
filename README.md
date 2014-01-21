This logger is written using c# .net version 4.0 (But should compile under 3.5)

This repo only contains the c# [visual studio 2013] project file for the logger.

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
			    try
			    {
				    MyLogger.Log("Test log line");

				    //These lines will be added to the queue which gets written from a seperate thread
				    MyLogger.QueueLog("Queued line");
				    MyLogger.QueueLog("Another Queued line");

				
				    //should throw DivideByZeroException
				    int num1 = 3;
				    int num2 = 0;
				    int answer = num1 / num2;
			    }
			    catch (Exception ex)
			    {
				    MyLogger.QueueLog("Here we can queue exception logs too", ex);
				    MyLogger.Log("Error Caught", ex);
			    }
		    }
	    }
    }
```