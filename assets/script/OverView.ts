import { _decorator, Component, Node, sys, Label } from 'cc';
import { GameInfoManager } from './GameInfoManager';
import { UIManager } from './UIManager';
const { ccclass, property } = _decorator;

@ccclass('OverView')
export class OverView extends Component {
    @property(Label)
    tipsLabel:Label = null;
    @property(Label)
    scoreLabel:Label = null;
    @property(Label)
    BestLabel:Label = null;
    start() {
        this.scoreLabel.string =  globalThis.curScore;
        this.BestLabel.string =  globalThis.bestScore;
        if(globalThis.curScore > globalThis.bestScore){
            console.log("新纪录！！！！！！");
            this.tipsLabel.string = "新纪录！"
            sys.localStorage.setItem("ac_bestScore", GameInfoManager.instance.score+"")
            // 然后更新
            globalThis.bestScore = globalThis.curScore;
        }else{
            this.tipsLabel.string = "再接再厉"
        }
    }

    update(deltaTime: number) {
        
    }
    reGameStart(){
        UIManager.instance.closeView("OverMenuView")
        globalThis.eventTarget.emit("resetItemFunc")
        globalThis.eventTarget.emit("reStart")
        // 发送重新更新item
    }
    onClose(){
        UIManager.instance.addView("StartMenuView")
        UIManager.instance.closeView("GameMainView")
        UIManager.instance.closeView("OverMenuView")
    }
}

