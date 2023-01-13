/*
 *  -------- Example Cloud Code Script --------
 *  Generate a random number within a range
 * --------------------------------------------
 */

/*
 * Include the lodash module for convenient functions such as "random"
 *  Check docs.unity.com/cloud-code/types-of-scripts.html#Available_libraries 
 *  to get a full list of libraries you can import in Cloud Code
 */
const _ = require("lodash-4.17");

/*
 * CommonJS wrapper for the script. It receives a single argument, which can be destructured into:
 *  - params: Object containing the parameters provided to the script, accessible as object properties
 *  - context: Object containing the projectId, environmentId, environmentName, playerId and accessToken properties.
 *  - logger: Logging client for the script. Provides debug(), info(), warning() and error() log levels.
 */
module.exports = async ({ params, context, logger }) => {
  // Log an info message with the parameters provided to the script and the invocation context
  logger.info("Script parameters: " + JSON.stringify(params));
  logger.info("Authenticated within the following context: " + JSON.stringify(context));

  const number = generateRandom(params.range);

  if (number > params.range) {
    // Log an error message with information about the exception
    logger.error("The number is greater than the range: " + number);
    // Return an error back to the client
    throw Error("Unable to generate the random nuber");
  }
  // Return the JSON result to the client
  return {
    sides: params.sides,
    number: number,
  };
};

// Functions can exist outside of the script wrapper
function generateRandom(range) {
  return _.random(1, range);
}

// In script parameters, it will be parsed and uploaded by UGS CLI
module.exports.params = {
    range: "NUMERIC"
};
