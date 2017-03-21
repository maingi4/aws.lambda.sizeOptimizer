#Automation with AWS Lambda sizing
This is a tool which finds the right size of your existing Lambda function programmatically by continously resizing it, firing test events at it until it finds the best size which would either:

 1. Save you the most money OR
 2. Give you the best performance

##Understanding how AWS Lambda scales

AWS Lambda service is simply, infrastructure which gets allocated to your function on demand as per need. When the need increases new infrastructure is automatically created internally which executes your function. The size of the unit of infrastructure is defined by you when you create the function, AWS allows us to select memory for the function and CPU allocation is directly proportional to the memory that you chose, what this means is that if you choose 128MB of memory you get x CPU while choosing 256MB gives you 2x of the same.

*[Read more](https://cloudncode.blog/2017/03/02/best-practices-aws-lambda-function/) at my blog.*

##Prerequisites
You need [.NET core installed](https://www.microsoft.com/net/core) wherever you run this tool, .NET core is open source and cross-platform so get one for your own OS.

The server / PC you run it on must either be running under a AWS Role having permissions to resize, get configurations and invoke Lambda functions or if you are on your PC, provide the credentials by keeping it in standard locations as [AWS recommends here](http://docs.aws.amazon.com/sdk-for-net/v2/developer-guide/net-dg-config-creds.html).

##Using the tool
You can run it in command line mode by simply giving some arguments, in .NET core command line is run via the dotnet.exe file:

    "C:\Program Files\dotnet\dotnet.exe" aws.lambda.sizeOptimizer.dll [-?|-h|--help] [-r |--aws-region <region>] [-n |--fn-name <functionName>] [-p |--payload-path <payloadPath>] [-64 |--use-64mb] [-l |--latency-margin <acceptableDropInPerformance>] [-ar |--auto-resize]

##Running it manually
Simply run the program without giving it any arguments and it will ask for input.

#Want to contribute?
I am open (and actually looking forward) to pull requests on the project.

##Setting up the environment
You need Visual Studio Code, Visual studio code is a free and open source code editor developed by Microsoft. It is cross-platform and works with Windows, MAC and Linux, for more features and background on this checkout this [wiki link](https://en.wikipedia.org/wiki/Visual_Studio_Code).

In my experience with this code editor, it is by far the best free code editor that I have worked with. Its ability to go multi-platform along with being free to use while supporting all the languages that I will ever use (with plugins installed) it is a go-to IDE for anyone on a budget or a requirement of cross-platform targeting.

If you don’t have .Net core installed get it [from here](https://www.microsoft.com/net/core#windowscmd).

Install the [c# extension for VS code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) from VS code marketplace.

You can optionally checkout a [getting started guide](https://cloudncode.blog/2017/01/24/getting-started-with-writing-and-debugging-aws-lambda-function-with-visual-studio-code/) on it on my blog.
