import { _decorator, Component, Node, resources, Prefab, Asset } from 'cc';
const { ccclass, property } = _decorator;

@ccclass('ResourcesManager')
export class ResourcesManager {
    private static _instance: ResourcesManager;
    static get instance () {
        if (this._instance) {
            return this._instance;
        }

        this._instance = new ResourcesManager();
        return this._instance;
    }
    init(){

    }
    // //异步加载直到资源加载完成
    // getResources<T extends typeof Asset>(url,type:T){
    //     const pre = new Promise((resolve, reject) => {
    //         resources.load(url, type,(err:Error, type: Asset) => {
    //             if(err){
    //                 console.log("err_getResources")
    //                 reject(err)
    //             }
    //             console.log("getResources")
    //             return
    //             resolve(type)
    //         })
    //     })
    //     return pre
    // }
    static async getRes<T extends typeof Asset>(url: string, type: T) {
        // // let premise = new Promise((resolve, reject) => {
        //     let prefab = await resources.load(url, type, (err: Error, res: any) => {
        //         if (!err) {
        //             console.log("resolve")
        //             // resolve(res);
        //         } else {
        //             // reject('UtilRes.loadSync url:' + url + ',err:' + err);
        //         }
        //     });
        // });
        // let prefab = await premise as T;
        // console.log(prefab)
        let data = await this.loadAsync(url,type);
        console.log(data)
        return data;
    }
    // public static async createActorNode(resUrl: string): Promise<cc.Node> {
    //     let [prefab, err] = await MiUtil.asyncWrap<cc.Prefab, string>(UtilRes.loadAsync(resUrl, cc.Prefab));
    //     if (err) {
    //         cc.error('MapUtil.createActor resUrl:' + resUrl + ',err:' + err);
    //         return null;
    //     }
    //     const node = cc.instantiate(prefab);
    //     return node;
    // }
    // static loadResSync<T extends typeof Asset>(url: string, type: T):Promise<InstanceType<T>> {
    //     return new Promise<any>((reovle, reject) => {
    //         resources.load(url, type, (error: Error, resource: Asset) => {
    //             if (error) {
    //                 reject(error);
    //             } else {
    //                 //console.log("loadResSync")
    //                 reovle(resource);
    //             }
    //         });
    //     });
    // }
    public static async loadAsync<T extends typeof Asset>(url: string, type: T): Promise<T> {
        return new Promise((resolve, reject) => {
            resources.load(url, type, (err: Error, res: any) => {
                if (!err) {
                    resolve(res);
                } else {
                    reject('UtilRes.loadSync url:' + url + ',err:' + err);
                }
            });
        });
    }
    // public static async asyncWrap<T, U = any>(promise: Promise<T>): Promise<[T | null, U | null]> {
    //     try {
    //         const data = await promise;
    //         const result: [T, null] = [data, null];
    //         return result;
    //     } catch (err) {
    //         const result_1: [null, U] = [null, err];
    //         return result_1;
    //     }
    // }
    // public static loadAsync<T extends typeof Asset>(url: string, type: T): Promise<InstanceType<T>> {
    //     return new Promise<any>((resolve, reject) => {
    //         resources.load(url, type, (err: Error, res: any) => {
    //             if (!err) {
    //                 console.log("loadAsync")
    //                 resolve(res);
    //             } else {
    //                 reject('UtilRes.loadSync url:' + url + ',err:' + err);
    //             }
    //         });
    //     });
    // }
}

