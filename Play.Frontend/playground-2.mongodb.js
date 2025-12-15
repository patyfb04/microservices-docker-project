// MongoDB Playground
// Use Ctrl+Space inside a snippet or a string literal to trigger completions.

// The current database to use.
use("Catalog");

db.getCollection("items")
  .find({})
  .forEach(function (doc) {
    var hex = doc._id.base64; // raw base64
    // decode base64 to hex string manually
    var raw = BinData(3, hex);
    var str = raw.hex(); // hex string of the GUID
    // format into canonical GUID string if needed
    doc._id = str;
    db.Items.insertOne(doc);
    db.Items.deleteOne({ _id: raw });
  });
