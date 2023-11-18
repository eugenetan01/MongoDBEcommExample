import spacy
import numpy as np
from sklearn.cluster import KMeans
from pymongo import MongoClient

# Load the spaCy English language model
nlp = spacy.load("en_core_web_sm")


def get_product_data_from_mongodb():
    # Connect to your MongoDB instance
    client = MongoClient(
        "mongodb+srv://sa:admin@experimental.g3aer.mongodb.net/?retryWrites=true&w=majority"
    )

    # Replace "your_collection" with the actual name of your collection
    collection = client.shengshiong.product_catalog

    # Retrieve product data from MongoDB
    # Modify this query based on your MongoDB schema
    product_data = collection.find()

    product_data_list = list(product_data)

    # Close the MongoDB connection
    client.close()

    return product_data_list


def extract_product_items(item_description):
    # Assuming that product items are comma-separated in the item_description
    product_items = item_description.split(",")

    # Remove leading and trailing whitespaces from each product item
    product_items = [item.strip() for item in product_items]

    return product_items


def apply_kmeans_embeddings(embeddings):
    # Reshape the 1D array to 2D array
    embeddings_2d = embeddings.reshape(-1, 1)

    # Apply K-Means clustering
    kmeans = KMeans(n_clusters=3, random_state=42)
    kmeans.fit(embeddings_2d)

    return kmeans.labels_


def generate_ai_category(product_item, cluster_label):
    # Combine the product item with the cluster label
    return f"{product_item} Category {cluster_label + 1}"


def main():
    # Fetch product data from MongoDB
    product_data = get_product_data_from_mongodb()

    if not product_data:
        print("No product data found in MongoDB.")
        return

    # Print the product data for debugging
    # print("Product Data:", product_data)

    # Extract product items from the document
    for product in product_data:
        product_items = extract_product_items(product.get("itemDescription", ""))

        if not product_items:
            print("No product items found.")
            return

        # Get embeddings from the data (assuming "itemDesc_embeddings" is present)
        embeddings = np.array(product.get("itemDesc_embeddings", []))

        if embeddings.size == 0:
            print("No embeddings found.")
            return

        # Apply K-Means clustering to embeddings
        labels = apply_kmeans_embeddings(embeddings)

        # Assign categories to the product items based on the clustering
        for product_item, cluster_label in zip(product_items, labels):
            category = generate_ai_category(product_item, cluster_label)
            print(f"Product Item: {product_item} | Category: {category}")


if __name__ == "__main__":
    main()
