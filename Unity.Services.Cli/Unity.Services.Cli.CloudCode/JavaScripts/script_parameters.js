#!/usr/bin/env node
const infiniteProxy = () => {
    return new Proxy(function() {}, {
        get: (_, p) => {
            if (p === "toJSON") {
                return () => { throw new Error("'required' resource might be used during script parsing") };
            } else {
                return infiniteProxy()
            }
        },
        apply: () => infiniteProxy(),
    });
};

function withPatchedEnv(fn){
    module = {};
    module.exports = exports = {};
    require = infiniteProxy();
    console = infiniteProxy();
    fn();
}

function scriptParameters(source){
    let parameters;
    withPatchedEnv(() => {
        eval(source);
        parameters = module?.exports?.params;
    });
    return JSON.stringify(parameters ?? null);
}

globalThis.scriptParameters = scriptParameters;
