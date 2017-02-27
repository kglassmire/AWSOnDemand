# AWSOnDemand Notes:

This project allows for:
+ Listing EC2 instances
+ Starting EC2 instances
+ Stopping EC2 instances
+ Listing RDS instances 
+ Snapshotting RDS instances
+ Deleting RDS instances
+ Restoring RDS instances from snapshot and time

# Example JSON to use:
{
  "Ec2Actions": "StartEc2Instances",
  "RdsActions": "ListRdsInstances",
  "Tag": "TagValue"
}

# Snapshot naming convention
snapshot-{dbinstanceidentifier}-{DateTime in yyyyMMddhhmm}
e.g. snapshot-myDb-20170227045824

# Logging Examples
```
START RequestId: 9cb03eb3-adad-11e6-a90b-f7b88886b41f Version: $LATEST
Running ec2 instances automation.
Looking for tags matching: {tag}
Found item: key: Name, value: {tag}, resouceId: {resourceId}, resourceType: instance
Returning instanceId: {instanceId}
Starting Ec2 instances with tag: {tag}
Starting instance:
{
    "CurrentState": {
        "Code": 0,
        "Name": {
            "Value": "pending"
        }
    },
    "InstanceId": "{instanceId}",
    "PreviousState": {
        "Code": 80,
        "Name": {
            "Value": "stopped"
        }
    }
}

Looking for tags matching: {tag}
Found db instance item: instanceArn: arn:aws:rds:us-east-1:11223123:db:{dbinstanceidentifier}, dbName: {dbinstanceIdentifier}, engine: ostgres
Listing Rds instances with tag: {tag}
Looking for tags matching: {tag}
Found db instance item: instanceArn: arn:aws:rds:us-east-1:3214123321321:db:{dbinstanceidentifier}, dbName: {dbinstanceidentifier}, engine: postgres
END RequestId: 9cb03eb3-adad-11e6-a90b-f7b88886b41f
REPORT RequestId: 9cb03eb3-adad-11e6-a90b-f7b88886b41f	Duration: 7396.04 ms	Billed Duration: 7400 ms Memory Size: 256 MB	Max Memory Used: 42 MB	
```


# AWS Lambda Empty Function Project

This starter project consists of:
* Function.cs - class file containing a class with a single function handler method
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS
* project.json - .NET Core project file with build and tool declarations for the Amazon.Lambda.Tools Nuget package

You may also have a test project depending on the options selected.

The generated function handler is a simple method accepting a string argument that returns the uppercase equivalent of the input string. Replace the body of this method, and parameters, to suit your needs. 

## Here are some steps to follow from Visual Studio:

To deploy your function to AWS Lambda, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed function open its Function View window by double-clicking the function name shown beneath the AWS Lambda node in the AWS Explorer tree.

To perform testing against your deployed function use the Test Invoke tab in the opened Function View window.

To configure event sources for your deployed function, for example to have your function invoked when an object is created in an Amazon S3 bucket, use the Event Sources tab in the opened Function View window.

To update the runtime configuration of your deployed function use the Configuration tab in the opened Function View window.

To view execution logs of invocations of your function use the Logs tab in the opened Function View window.

## Here are some steps to follow to get started from the command line:

Once you have edited your function you can use the following command lines to build, test and deploy your function to AWS Lambda from the command line (these examples assume the project name is *EmptyFunction*):

Restore dependencies
```
    cd "EmptyFunction"
    dotnet restore
```

Execute unit tests
```
    cd "EmptyFunction/test/EmptyFunction.Tests"
    dotnet test
```

Deploy function to AWS Lambda
```
    cd "EmptyFunction/src/EmptyFunction"
    dotnet lambda deploy-function
```
