using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.RDS;
using Amazon.RDS.Model;
using Newtonsoft.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSOnDemand
{
    public class Function
    {
        private AmazonEC2Client _ec2Client;
        private AmazonRDSClient _rdsClient;
        private AWSAutomation _awsAutomation;

        public Function()
        {
            _ec2Client = new AmazonEC2Client();            
            _rdsClient = new AmazonRDSClient();
        }

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="awsAutomation"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool FunctionHandler(AWSAutomation awsAutomation, ILambdaContext context)
        {
            bool result;
            Console.WriteLine("Running ec2 instances automation.");

            result = ProcessEc2Instances(awsAutomation);
            return true;
        }

        private bool ProcessEc2Instances(AWSAutomation awsAutomation)
        {
            // find EC2 instances with tags we need.
            var listEc2Instances = FindEC2InstancesWithSpecifiedTag(awsAutomation.Tag);
            if (listEc2Instances.Count == 0)
                return false;

            // stop those instances
            if (awsAutomation.Ec2Actions == Ec2Actions.StartEC2Instances)
            {
                Console.WriteLine("Starting instances with tag: {0}", awsAutomation.Tag);
                StartEc2Instances(listEc2Instances);
            }
            else if (awsAutomation.Ec2Actions == Ec2Actions.StopEC2Instances)
            {
                Console.WriteLine("Stopping instances with tag: {0}", awsAutomation.Tag);
                StopEc2Instances(listEc2Instances);
            }
            
            return true;
        }

        private bool StopEc2Instances(List<string> instanceIdsToStop)
        {
            bool success = true;
            
            try
            {
                var stopInstancesRequest = new StopInstancesRequest(instanceIdsToStop);
                var stopInstancesResponse = _ec2Client.StopInstancesAsync(stopInstancesRequest, CancellationToken.None);
                if (stopInstancesResponse != null)
                {
                    foreach (var item in stopInstancesResponse.Result.StoppingInstances)
                    {
                        string itemToString = JsonConvert.SerializeObject(item);
                        Console.WriteLine("Stopping instance: {0}", itemToString);
                    }
                }
            }
            catch (Exception e)
            {
                success = false;
                Console.WriteLine(e);               
            }

            return success;
        }

        private bool StartEc2Instances(List<string> instanceIdsToStart)
        {
            bool success = true;
            try
            {
                var startInstancesRequest = new StartInstancesRequest(instanceIdsToStart);
                var startInstancesResponse = _ec2Client.StartInstancesAsync(startInstancesRequest, CancellationToken.None);

                foreach (var item in startInstancesResponse.Result.StartingInstances)
                {
                    string itemToString = JsonConvert.SerializeObject(item);
                    Console.WriteLine("Starting instance: {0}", itemToString);
                }
            }
            catch (Exception e)
            {
                success = false;
                Console.WriteLine(e);
            }

            return success;
        }

        private List<String> FindEC2InstancesWithSpecifiedTag(string input)
        {
            var instanceResourceIds = new List<String>();

            Console.WriteLine("Looking for tags matching: {0}", input);
            var describeTagsResponse = _ec2Client.DescribeTagsAsync(CancellationToken.None);

            var itemsWithSpecifiedTag =
                describeTagsResponse.Result.Tags.Where(
                    x =>
                        x.Key.Equals("Name", StringComparison.OrdinalIgnoreCase) &&
                        x.Value.Equals(input, StringComparison.OrdinalIgnoreCase) &&
                        x.ResourceType == ResourceType.Instance);

            if (!itemsWithSpecifiedTag.Any())
            {
                Console.WriteLine("No items found matching the specified tag."); 
                return new List<String>();
            }

            foreach (var item in itemsWithSpecifiedTag)
            {
                Console.WriteLine($"Found item: key: {item.Key}, value: {item.Value}, resouceId: {item.ResourceId}, resourceType: {item.ResourceType}");                    
                instanceResourceIds.Add(item.ResourceId);
            }

            foreach (var item in instanceResourceIds)
            {
                Console.WriteLine("Returning instanceId: {0}", item);
            }

            return instanceResourceIds;
        }

        private List<DBInstance> FindRdsInstancesWithSpecifiedTag(string input)
        {
            var instanceResourceIds = new List<String>();

            Console.WriteLine("Looking for tags matching: {0}", input);

            var describeDbInstancesResponse = _rdsClient.DescribeDBInstancesAsync();
            List<DBInstance> dbInstances = new List<DBInstance>();
            foreach (var dbInstanceItem in describeDbInstancesResponse.Result.DBInstances)
            {
                var listTagsForResourceRequest = new ListTagsForResourceRequest
                {
                    ResourceName = dbInstanceItem.DBInstanceArn
                };

                var listTagsForResourceResponse = _rdsClient.ListTagsForResourceAsync(listTagsForResourceRequest,
                    CancellationToken.None);

                var tag =
                    listTagsForResourceResponse?.Result.TagList.Where(
                        x =>
                            x.Key.Equals("name", StringComparison.OrdinalIgnoreCase) &&
                            x.Value.Equals(input, StringComparison.OrdinalIgnoreCase));

                if (tag != null)
                    dbInstances.Add(dbInstanceItem);
            }

            foreach (var item in dbInstances)
            {
                Console.WriteLine("Found db instance item: instanceArn: {0}, dbName: {1}, engine: {2}",
                    item.DBInstanceArn, item.DBInstanceIdentifier, item.Engine);
            }

            return dbInstances;
        }

        private bool CreateRdsInstanceFromSnapshots(List<DBSnapshot> snapshots)
        {
            bool success = true;
            foreach (var item in snapshots)
            {
                try
                {
                    RestoreDBInstanceFromDBSnapshotRequest restoreDbInstanceFromDbSnapshotRequest = new RestoreDBInstanceFromDBSnapshotRequest(item.DBInstanceIdentifier, item.DBSnapshotIdentifier);
                    _rdsClient.RestoreDBInstanceFromDBSnapshotAsync(restoreDbInstanceFromDbSnapshotRequest,
                        CancellationToken.None);
                }
                catch (Exception e)
                {
                    success = false;
                    Console.WriteLine(e);                    
                }
            }

            return success;
        }

        private bool DeleteRdsInstance(List<DBInstance> dBInstances)
        {
            bool success = true;
            foreach (var item in dBInstances)
            {
                try
                {
                    var deleteDbInstanceRequest = new DeleteDBInstanceRequest(item.DBInstanceIdentifier);
                    var deleteDbInstanceResponse = _rdsClient.DeleteDBInstanceAsync(deleteDbInstanceRequest, CancellationToken.None);                    
                }
                catch (Exception e)
                {
                    success = false;
                    Console.WriteLine(e);
                }
            }

            return success;
        }

        private bool CreateDbSnapshots(List<DBInstance> dbInstances)
        {
            bool success = true;
            foreach (var item in dbInstances)
            {
                try
                {
                    var createDbSnapshotRequest =
                        new CreateDBSnapshotRequest(
                            String.Format("snapshot-{0}-{1}", item.DBInstanceIdentifier,
                                DateTime.Now.ToString("yyyyMMddHHmmss")), item.DBInstanceIdentifier);
                    _rdsClient.CreateDBSnapshotAsync(createDbSnapshotRequest, CancellationToken.None);
                }
                catch (Exception e)
                {
                    success = false;
                    Console.WriteLine(e);
                }
            }

            return success;
        }

        private DBSnapshot FindLatestSnapshot(List<DBSnapshot> snapshots, DateTime? specifiedTime = null)
        {
            // if we don't have a value specified, just set max and it will get the latest snapshot
            if (!specifiedTime.HasValue)
                specifiedTime = DateTime.MaxValue;

            return snapshots.OrderByDescending(x => x.SnapshotCreateTime <= specifiedTime.Value).FirstOrDefault();
        }

        private List<DBSnapshot> FindRdsSnapshotsWithSpecifiedTag(string input)
        {
            bool success = true;

            var describeDbSnapshotsResponse = _rdsClient.DescribeDBSnapshotsAsync(CancellationToken.None);

            List<DBSnapshot> dbSnapshots = new List<DBSnapshot>();
            foreach (var dbSnapshotItem in describeDbSnapshotsResponse.Result.DBSnapshots)
            {
                var listTagsForResourceRequest = new ListTagsForResourceRequest
                {
                    ResourceName = dbSnapshotItem.DBSnapshotArn
                };

                var listTagsForResourceResponse = _rdsClient.ListTagsForResourceAsync(listTagsForResourceRequest,
                    CancellationToken.None);

                var tag =
                    listTagsForResourceResponse?.Result.TagList.Where(
                        x =>
                            x.Key.Equals("name", StringComparison.OrdinalIgnoreCase) &&
                            x.Value.Equals(input, StringComparison.OrdinalIgnoreCase));

                if (tag != null)
                    dbSnapshots.Add(dbSnapshotItem);
            }

            foreach (var item in dbSnapshots)
            {
                Console.WriteLine("Found db instance item: instanceArn: {0}, dbName: {1}, engine: {2}",
                    item.DBSnapshotArn, item.DBInstanceIdentifier, item.Engine);
            }

            return dbSnapshots;
        }

        public class AWSAutomation
        {
            public Ec2Actions Ec2Actions { get; set; }            
            public RdsActions RdsActions { get; set; }
            public string Tag { get; set; }
        }

        public enum Ec2Actions
        {
            StartEC2Instances,
            StopEC2Instances
        }

        public enum RdsActions
        {
            RestoreRDSInstances,
            DeleteRDSInstances
        }
    }



}
