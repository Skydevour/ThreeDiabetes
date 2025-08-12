import { _decorator, Component, Node, Layers, JsonAsset, resources, instantiate, Prefab, tween } from 'cc';
const { ccclass, property } = _decorator;
import UIDef from "../resources/json/UIDef.json";
@ccclass('UIManager')
export class UIManager {
    private static _instance: UIManager;
    private _uiNode?: Node;

    private loadingNode: Node;
    private messageNode: Node;
    private guideNode: Node;
    private popNode: Node;
    private gameNode: Node;

    private viewList:Map<string,boolean> = new Map();

    static get instance () {
        if (this._instance) {
            return this._instance;
        }

        this._instance = new UIManager();
        return this._instance;
    }
     /**管理器初始化*/
     init (uiNode: Node) {
        this._uiNode = uiNode;
        uiNode.destroyAllChildren();
        //渲染顺序倒序
        
        //游戏界面
        this.initViewNode('gameNode')
        //pop界面
        this.initViewNode('popNode')
        //guide 新手指引界面
        this.initViewNode('guideNode')
        //msg 系统消息界面
        this.initViewNode('messageNode')
        //loading 加载界面
        this.initViewNode('loadingNode')
    }
    private initViewNode(name:string){
        this[name] = new Node(name);
        this[name].layer = Layers.Enum.UI_2D;
        this[name].parent = this._uiNode;
    }
    //添加界面
    addView(viewName:string){
        //获取界面数据
        //console.log(typeof this.viewList)
        let viewData = UIDef[viewName];
        //如果界面数据存在，就说明界面合法，添加界面到队列数据
        if (!viewData){
            return
        }
        //console.log(viewData)
        this.viewList.set(viewName,true);
        //如果该界面不存在，创建该界面，设置界面为当前分组最上层，并处理下一个数据
        resources.load('Perfab/' + viewData.src, (err, Prefab:Prefab) => {
            //创建节点
            //添加到对应的位置上
            if (err) {
                console.error(err);
                return;
            }
            //不需要创建界面了
            if (!this.viewList.get(viewName)){
                return;
            }

            let viewNode:Node = this[viewData.group].getChildByName(viewName)
            //如果该界面存在，设置界面为当前分组最上层，并处理下一个数据
            if (viewNode){
                viewNode.setSiblingIndex(-1);
                return;
            }
            const newNode = instantiate(Prefab);
            newNode.name = viewName;
            newNode.parent = this[viewData.group];
            //层级划分
            newNode.setSiblingIndex(-1);
        })
    }
    
    //关闭界面
    closeView(viewName:string){
        //获取界面数据
        let viewData = UIDef[viewName];
        //如果界面数据存在，且允许移除，准备移除界面
        if (!viewData || viewData.isForever){
            return
        }
        this.viewList.set(viewName,false);
        //找到所在层级
        let viewNode:Node = this[viewData.group].getChildByName(viewName)
        //找到对应界面
        //清除界面
        if (viewNode){
            viewNode.destroy();
        }
    }
    getView(viewName:string):Node {
        let viewData = UIDef[viewName];
        if (!viewData){
            return;
        }
        return this[viewData.group].getChildByName(viewName);
    }
    //清除所有界面
    closeAllView(){
        //直接清除
        for (let [viewName, isActive] of this.viewList) {
            if (isActive) {
                this.closeView(viewName)
            }            
        }
    }
    onDestroy(){
        //UIDef = null;
    }
}

