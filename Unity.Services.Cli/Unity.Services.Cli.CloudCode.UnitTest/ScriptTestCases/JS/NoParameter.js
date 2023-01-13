/*
 *  -------- Example Cloud Code Script --------
 *  Roll a 6-sided dice and get back the result
 * --------------------------------------------
 */

/*
 * Include the lodash module for convenient functions such as "random"
 *  Check docs.unity.com/cloud-code/types-of-scripts.html#Available_libraries
 *  to get a full list of libraries you can import in Cloud Code
 */
const _ = require("lodash-4.17");

const NUMBER_OF_SIDES = 6;

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

    const roll = rollDice(NUMBER_OF_SIDES);

    if (roll > NUMBER_OF_SIDES) {
        // Log an error message with information about the exception
        logger.error("The roll is greater than the number of sides: " + roll);
        // Return an error back to the client
        throw Error("Unable to roll dice!");
    }
    // Return the JSON result to the client
    return {
        sides: NUMBER_OF_SIDES,
        roll: roll,
    };
};

// Functions can exist outside of the script wrapper
function rollDice(sides) {
    return _.random(1, sides);
}
