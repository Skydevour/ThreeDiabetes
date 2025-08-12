import { _decorator, Component, Node, AudioSource, resources, AudioClip, EventTouch, assert, director, Asset, Prefab, EventTarget, sys, instantiate, find } from 'cc';
const { ccclass, property } = _decorator;
import { AudioManager } from './AudioManager'
import { UIManager } from "./UIManager";
import { ResourcesManager } from "./ResourcesManager";
@ccclass('GameApp')
export class GameApp extends Component {
    @property(Node)
    private uiNode: Node = null;
    private _audioSource: AudioSource = null;
    onLoad() {
        const audioSource = this.node.getComponent(AudioSource)!;
        assert(audioSource);
        this._audioSource = audioSource;
        // 声明常驻根节点，该节点不会在场景切换中被销毁。目标节点必须是根节点，否则无效。
        director.addPersistRootNode(this.node);

        // 将节点封装到管理器中
        // 声音管理器
        AudioManager.instance.init(this._audioSource);
        // UI管理器
        UIManager.instance.init(this.uiNode);
        // 资源管理器
        ResourcesManager.instance.init();
        // 自定义事件监听器
        if (!globalThis.eventTarget) {
            globalThis.eventTarget = new EventTarget();
        }
        if (!globalThis.bestScore) {
            sys.localStorage.setItem("ac_bestScore", "0")
            // console.log("globalThis.bestScore"+sys.localStorage.getItem("ac_bestScore"));
            globalThis.bestScore = sys.localStorage.getItem("ac_bestScore") || 0;
            console.log("globalThis.bestScore" + globalThis.bestScore);
        }
    }
    start() {
        // let asst:Prefab ;
        //  let asst = ResourcesManager.loadAsync('Perfab/MenuView',Prefab);
        // //ResourcesManager.instance.getRes('Perfab/MenuView',Prefab);
        // asst.then(function(res){
        //     const newNode = instantiate(res);
        //     newNode.parent = find("Canvas")
        //     console.log("addView");
        //     //newNode.
        // })
        // console.log("create over")
        // console.log();
        // console.log(a);
        // let asst1 = ResourcesManager.instance.getResources('Perfab/MenuView',Prefab);
        // console.log(asst1);
        // let a = ResourcesManager.getRes('Perfab/MenuView',Prefab);
        // console.log(a);
        //asst.
        UIManager.instance.addView("StartMenuView")
        //UIManager.instance.addView("BgView")
        //UIManager.instance.closeView("MenuView")
        // let promise = new Promise(function(resolve,reject){
        //     console.log("Promise create");
        //     setTimeout(resolve,1000);
        //    // (null);
        // });

        // promise.then(function(){

        //     console.log('resolved.');
        // });

        // console.log("Hi!");

        //Promise create
        //Hi!
        //resolved.
    }

    update(deltaTime: number) {
    }
    onDestroy() {
        UIManager.instance.onDestroy()
    }

}

