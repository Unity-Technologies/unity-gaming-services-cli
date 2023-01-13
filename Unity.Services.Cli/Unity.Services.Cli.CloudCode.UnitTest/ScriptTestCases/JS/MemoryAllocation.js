const test = (array) => {
    array.push((new Array(1000000)).fill("test"));
};

const testArray = [];

for(let i = 0; i <= 1000; i++) {
    test(testArray);
}

module.exports.params = {
    data: testArray
}
