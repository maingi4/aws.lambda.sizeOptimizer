#Automation with AWS Lambda sizing
This is a tool which finds the right size of your existing Lambda function programmatically by continously resizing it, firing test events at it until it finds the best size which would either:

 1. Save you the most money OR
 2. Give you the best performance

##Understanding how AWS Lambda scales

AWS Lambda service is simply, infrastructure which gets allocated to your function on demand as per need. When the need increases new infrastructure is automatically created internally which executes your function. The size of the unit of infrastructure is defined by you when you create the function, AWS allows us to select memory for the function and CPU allocation is directly proportional to the memory that you chose, what this means is that if you choose 128MB of memory you get x CPU while choosing 256MB gives you 2x of the same.

*[Read more](https://cloudncode.blog/2017/03/02/best-practices-aws-lambda-function/) at my blog.*

##Using the tool
command line examples here

##Running it manually
console input options here

#Want to contribute?
Setting up the environment here.