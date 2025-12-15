// MongoDB Playground
// Use Ctrl+Space inside a snippet or a string literal to trigger completions.

// The current database to use.
use("Identity");

// Find a document in a collection.
// Connect to your database first, e.g. use play_identity
db.Users.find({ Roles: { $elemMatch: { $type: "string" } } }).forEach(function (
  user
) {
  print("Fixing user: " + user.UserName);

  var fixedRoles = [];
  user.Roles.forEach(function (roleStr) {
    // Remove dashes and lowercase
    var hex = roleStr.replace(/-/g, "").toLowerCase();
    fixedRoles.push(BinData(3, hex));
  });

  db.Users.updateOne({ _id: user._id }, { $set: { Roles: fixedRoles } });
});
