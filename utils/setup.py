import pandas as pd
import numpy as np

# Read the CSV file
df = pd.read_csv("Groceries_dataset.csv", delimiter=",")

# Rename 'Member_number' to 'product_id'
df = df.rename(columns={"Member_number": "product_id"})

# Remove the 'Date' column
df = df.drop(columns=["Date"])

# Generate a 'stock' field with a random number between 1 and 100 for each row
df["stock"] = np.random.randint(1, 101, size=len(df))

# Remove duplicates based on 'itemDescription'
df = df.drop_duplicates(subset=["itemDescription"])

# Save the result to a new CSV file
df.to_csv("product_catalog.csv", index=False)

print("Processing complete. Output saved to 'output.csv'.")
