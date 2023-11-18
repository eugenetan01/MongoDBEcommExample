# MongoDB Ecommerce Example

__1. The ability to do product recommendations__ <br/>
__2. The ability to search for related products (i.e. searching for “short grain” rice and getting “japanico” rice)__ <br/>
__3. The ability to encrypt sensitive PII data (e.g. credit card numbers / cvc / mobile phone) when stored in the cloud__ <br/>

__SA Maintainer__: [Eugene Tan](mailto:eugene.tan@mongodb.com) <br/>
__Time to setup__: 30 mins <br/>
__Time to execute__: 15 mins <br/>

---
## Description

This proof shows how to use vector search to look for products of similar semantics to achieve product recommendations using MongoDB. It also showcases how to use Lucene to surface related products via innate categories / similar keywords.
The proof also showcases queryable encryption and how to store PII data in encrypted form on MongoDB and allow it to be queried and processed without decryption except on the app layer.


---
## Setup
__1. Configure Laptop__
* Ensure MongoDB version 7.0+ is installed on your laptop in order to access the MongoDB command line tools (a MongoDB Atlas cluster will be used to actually host the data)
* [Download](https://www.mongodb.com/download-center/compass) and install Compass on your laptop
* Ensure C# (DotNet version 7+) and NPM are installed your laptop
* Ensure Visual Studio / Visual Studio Code with C# DotNet support installed 

__2. Configure Atlas Environment__
* Log-on to your [Atlas account](http://cloud.mongodb.com) (using the MongoDB SA preallocated Atlas credits system) and navigate to your SA project
* In the project's Security tab, choose to add a new user, e.g. __main_user__, and for __User Privileges__ specify __Read and write to any database__ (make a note of the password you specify)
* In the Security tab, add a new __IP Whitelist__ for your laptop's current IP address
* Create an __M10__ based 3 node replica-set in a single cloud provider region of your choice with default settings
* In the Atlas console, for the database cluster you deployed, click the __Connect button__, select __Connect Your Application__, and for the __latest C# version__ copy the __Connection String Only__ - make a note of this MongoDB URL address to be used in the next step

__3. Load Data Into A Collection In The Atlas Cluster__
* Load the data in the 0. Setup folder by running mongorestore 

  ```bash
  mongorestore --uri mongodb+srv://sa:admin@shengshiong.g3aer.mongodb.net/
  ```
Paste the copied URI into the `--uri` parameter, replacing the username & password fields with those you created earlier.
<br/><br/>
__Note__: This process generates over 1GB of data. The `mgeneratejs` process itself only takes ~10 minutes to complete but the upload process can take significantly longer if you're using a slow network connection.

__4. Check That The Collection's Data Can Be Accessed Using Compass__
* From the Atlas console click the __Connect button__, select __Connect With MongoDB Compass__ and click the __Copy__ button to copy the connection string
* Launch Compass and when prompted select to use the __MongoDB Connection String__ detected from the clipboard, fill in the __Username__ and __Password__ fields as required and then click the __Connect__ button

__5. Setup MongoDB environment (playground) on Vscode and connect to your cluster there__

__6. Create the following indexes on the product_catalogue collection__

* For scenario 1. PR-VectorSearch
```
{
  "mappings": {
    "dynamic": true,
    "fields": {
      "itemDesc_embeddings": {
        "dimensions": 384,
        "similarity": "dotProduct",
        "type": "knnVector"
      }
    }
  }
}
```
* For scenario 3. Synonyms
```
{
  "mappings": {
    "dynamic": false,
    "fields": {
      "itemDescription": {
        "type": "string"
      }
    }
  },
  "synonyms": [
    {
      "analyzer": "lucene.standard",
      "name": "synonym_mapping",
      "source": {
        "collection": "product_synonyms"
      }
    }
  ]
}
```

__7. Create the following indexes on the product_catalog_categories collection - for scenario 2. MoreLikeThis__

```
{
  "mappings": {
    "dynamic": true
  }
}
```

__9. Download and install Automatic encryption shared library from [here](https://www.mongodb.com/docs/manual/core/queryable-encryption/reference/shared-library/)__

__8. Edit appsettings.json to point to your local mongo_crypt shared library folder where lib/mongo_crypt_v1.dylib is stored__

---
## Execution

__1. Navigate to the 1.PR-VectorSearch folder__
* Open the code in Visual Studio
* Show that in compass - search for the keyword: {itemDescription: "orange"}
* Show that there are no results where product is "orange"
* Show on line 62 of program.cs that we are searching for orange and accessing the vector embeddings using vector search on lines 81 - 92
* Run the program.cs and show that similar products to orange were returned using vector search 

__2. Navigate to the 2. MoreLikeThis folder__
* Show in compass that in the "product_catalog_categories" collection, there are different "rice" products
* Show that short grain rice and japanico rice are in the "SGrice" category
* Show that multi grain rice is in the "rice" category
* Show the search query is looking for items with Short grain keyword and in the SGRice category
* Show that Japanico and Short grain came back as top 2 results with highest relevance although "short grain" is not present in the keyword "japanico rice"

__3. Navigate to the 3. Synonyms folder__
* Navigate to Atlas -> Data explorer and show the product_synonyms collection
* Show that there is an equivalent synonym where turkey and ham mean the same thing
* Navigate to 3. Synonyms folder and copy the command in commands.txt
* Click "Edit search query"
* Navigate to search query tester and paste in the command into the editor and run search
* SHow that both turkey and ham returned as a result although only search for turkey

__4. Navigate to the 4. Queryable Encryption folder__
* Show Line 44 and 72 we are dropping the collections if they already exist 
* Show the code in QueryableEncryptionTutorial.cs where line 47 - 69 is the schema for fields (PII fields namely ssn and credit card number) that are to be encrypted
* Show Line 79 we are grabbing the key to generate a new data encryption key
* Show lines 83 - 94 we are using the CMK to create a new collection with the new DEK (Data encryption key) according to the specified schema
* Show lines 104 - 120 a new record with new credit card number is inserted
* Show Line 129 trying to retrieve the same credit card record inserted
* Run the program.cs  

__5. Navigate to the 5. Auto HA folder__

* From a separate terminal/shell, execute the Python scripts to start continuously inserting records into and from the Atlas deployed database collection __AUTO_HA.records__, specifying the parameter for mongodb URI (include retryWrites and retryReads equal to false in the URL you provide here), e.g.:
  ```bash
  ./continuous-insert.py 'mongodb+srv://<username>:<password>@testcluster-abcd.mongodb.net?retryWrites=true'
  ```
* View the terminal/shell output of the Python scripts to check it has successfully connected to the Atlas database and is reporting that records are being inserted and read
* From the Atlas console, select the __.../Test Failover__ option to force a failure of the replica-set primary server; the Atlas console will then show a dialog similar to the following:

![atlasfailover](img/atlasfailover.png "atlasfailover")

* If the failure is detected and DB connection error is found, it will be written to the "tracker.txt" file - lines 56-63 at the exact time an error was found and when the driver reconnected to the DB

---
## Measurement for scenario 4

__1. Go to Compass and show that the fields are all encrypted and cannot be accessed__

__2. View the terminal output after program.cs runs to see the retrieved document - show that the values are decrypted and visible only to the application__

---
## Measurement for scenario 5

After using the Atlas console's __Test Failure__ feature (when the _retryable writes_ feature is employed), look for connection error data similar to the following in the "tracker.txt":
  ```
  2019-01-01 18:27:27.148666 - INSERT 4110
  2019-01-01 18:27:27.828104 - INSERT 4140
  2019-01-01 18:27:28.497667 - INSERT 4170
  2019-01-01 18:27:29.093474 - DB-CONNECTION-PROBLEM: connection closed
  2019-01-01 18:27:31.534841 - RECONNECTED-TO-DB
  2019-01-01 18:27:31.599981 - INSERT 4200
  2019-01-01 18:27:32.272013 - INSERT 4230
  2019-01-01 18:27:32.909000 - INSERT 4260
  ```

There should be no errors logged because retrywrites=true is enabled, ensuring no exceptions or downtime is experienced by the application even when a server went down and a failover happened on the MongoDB Atlas cluster
