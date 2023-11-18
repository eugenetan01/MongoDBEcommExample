#!/usr/bin/env python3
##
# Script to continuously inserts new documents into the MongoDB database/
# collection 'AUTO_HA.records'
#
# Prerequisite: Install latest PyMongo driver and other libraries, e.g:
#   $ sudo pip3 install pymongo dnspython
#
# For usage details, run with no params (first ensure script is executable):
#   $ ./continuous-insert.py
##
import sys
import random
import time
import datetime
import pymongo
import json

####
# Main start function
####


def main():
    print("")

    retry = True

    # retry writes
    uri = "mongodb+srv://sa:admin@shengshiong.g3aer.mongodb.net/?retryWrites=true&w=majority"
    peform_inserts(uri, retry)


####
# Perform the continuous database insert workload, sleeping for 10 milliseconds
# between each insert operation
####
def peform_inserts(uri, retry):
    mongodb_url = uri
    count = 0
    connect_problem = False
    print(f"Connecting to:\n {mongodb_url}\n")
    connection = pymongo.MongoClient(mongodb_url, retryWrites=retry, retryReads=retry)
    db = connection["AutoHA"]

    while True:
        try:
            count += 1
            db.records.insert_one(
                {"val": count, "date_created": datetime.datetime.utcnow()}
            )

            if count % 30 == 0:
                print(f"{datetime.datetime.now()} - INSERTED TILL {count}")

            if connect_problem:
                print(f"{datetime.datetime.now()} - RECONNECTED-TO-DB")
                f = open("tracker.txt", "a")
                f.write(f"{datetime.datetime.now()} - RECONNECTED-TO-DB")
                f.write("\n")
                f.close()
                connect_problem = False
            else:
                time.sleep(0.01)
        except KeyboardInterrupt:
            print
            sys.exit(0)
        except Exception as e:
            print(f"{datetime.datetime.now()} - DB-CONNECTION-PROBLEM: " f"{str(e)}")
            f = open("tracker.txt", "a")
            f.write(f"{datetime.datetime.now()} - DB-CONNECTION-PROBLEM: " f"{str(e)}")
            f.write("\n")
            f.close()
            connect_problem = True


####
# Print out how to use this script
####
def print_usage():
    print("\nUsage:")
    print("$ ./continuous-insert.py <mongodb_uri> <retry>")
    print("\nExample: (run script WITHOUT retryable writes enabled)")
    print(
        "$ ./continuous-insert.py mongodb+srv://<username>:<password>@<hostname>/<authDB>"
    )
    print("$or")
    print(
        "$ ./continuous-insert.py mongodb://<username>:<password>@<hostname1>:<port1>,<hostname2>:<port2>,<hostname3>:<port3>/<authDB>?replicaSet=<rsName>"
    )
    print("\nExample: (run script WITH retryable writes enabled):")
    print(
        "$ ./continuous-insert.py mongodb+srv://<username>:<password>@<hostname>/<authDB>?retryWrites=true retry"
    )
    print("$or")
    print(
        "$ ./continuous-insert.py mongodb://<username>:<password>@<hostname1>:<port1>,<hostname2>:<port2>,<hostname3>:<port3>/<authDB>?replicaSet=<rsName>&retryWrites=true retry"
    )
    print()


# Constants
DB_NAME = "eKYC"
TTL_INDEX_NAME = "date_created_ttl_index"


####
# Main
####
if __name__ == "__main__":
    main()
