import requests
import pymongo

client = pymongo.MongoClient(
    "mongodb+srv://sa:admin@experimental.g3aer.mongodb.net/?retryWrites=true&w=majority"
)
db = client.shengshiong
collection = db.product_catalog

hf_token = "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"
embedding_url = "https://api-inference.huggingface.co/pipeline/feature-extraction/sentence-transformers/all-MiniLM-L6-v2"


def generate_embedding(text: str) -> list[float]:
    response = requests.post(
        embedding_url,
        headers={"Authorization": f"Bearer {hf_token}"},
        json={"inputs": text},
    )

    if response.status_code != 200:
        raise ValueError(
            f"Request failed with status code {response.status_code}: {response.text}"
        )

    return response.json()


def load_embeddings_to_mongodb():
    for doc in collection.find({"itemDescription": {"$exists": True}}).limit(50):
        doc["itemDesc_embeddings"] = generate_embedding(doc["itemDescription"])
        collection.replace_one({"_id": doc["_id"]}, doc)


def issue_query():
    query = "apple"

    results = collection.aggregate(
        [
            {
                "$vectorSearch": {
                    "queryVector": generate_embedding(query),
                    "path": "itemDesc_embeddings",
                    "numCandidates": 100,
                    "limit": 4,
                    "index": "default",
                }
            }
        ]
    )
    for document in results:
        print(f'Product: {document["itemDescription"]}\n')


# print(generate_embedding("MongoDB is awesome"))
issue_query()

# load_embeddings_to_mongodb()
