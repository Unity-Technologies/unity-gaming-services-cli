var fs = require("fs");

const text = "Number"
fs.writeFileSync('FileShouldNotExist.txt', text);
const data = fs.readFileSync('FileShouldNotExist.txt', {encoding:'utf8', flag:'r'});
module.exports.params = {
    type: data,
    text: "Sample Text"
}
