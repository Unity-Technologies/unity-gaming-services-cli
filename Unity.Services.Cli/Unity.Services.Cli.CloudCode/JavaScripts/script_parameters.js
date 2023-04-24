const infiniteProxy = () => {
    return new Proxy(function() {}, {
        get: (_, p) => {
            if (p === "toJSON") {
                return () => { throw new Error("\"required\" resource might be used during script parsing") };
            } else {
                return infiniteProxy()
            }
        },
        apply: () => infiniteProxy(),
    });
};

function withPatchedEnv(fn){
    const tmpModule = module;
    const tmpRequire = require;
    const tmpConsole = console;

    try{
        module = {};
        module.exports = exports = {};
        require = infiniteProxy();
        console = infiniteProxy();

        fn();
    } catch (e) {
        tmpConsole.error(e);
    }
    finally {
        module = tmpModule;
        require = tmpRequire;
        console = tmpConsole;
    }
}

function scriptParameters(source){
    let parameters;
    withPatchedEnv(() => {
        eval(source);
        parameters = module?.exports?.params;
    });
    return parameters;
}
exports.scriptParameters = scriptParameters;

if (require.main === module) {
    let code ="";
    process.stdin.resume();
    process.stdin.on("data", data=> {
        code +=data.toString();
    });
    process.stdin.on('end', _ => {
        const serialized = JSON.stringify(scriptParameters(code));
        if(serialized){
            console.log(serialized);
        }
    });
}
