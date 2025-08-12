import { _decorator, Component, Node, input, Input, EventKeyboard, KeyCode, Label, PolygonCollider2D, Collider2D, Contact2DType, UI, Animation, System, sys, instantiate, random, debug, Button, Layout, SpriteFrame } from 'cc';
import { UIManager } from './UIManager';
const { ccclass, property,integer } = _decorator;

@ccclass('GameView')
export class GameView extends Component {
    @property(Node)
    player:Node = null;
    @property(Node)
    bg1:Node = null;
    @property(Node)
    bg2:Node = null;
    @property(Node)
    gray:Node = null;
    @property(Label)
    scoreLabel:Label = null;
    @integer
    bgSpeed = 0;
    @integer
    playerSpeed = 0;
    playerDir:KeyCode = null;
    score:number = 0;
    isOver:boolean = false;
    list:Array<Node> = [];
    onLoad(){
        input.on(Input.EventType.KEY_DOWN,this.startDir,this)
        input.on(Input.EventType.KEY_UP,this.stopDir,this)
        this.player.getComponent(PolygonCollider2D).on(Contact2DType.BEGIN_CONTACT,this.beginContact,this)
        globalThis.eventTarget.on("reStart",this.reGameStart,this)
    }
    start() {
        this.reGameStart()
    }
    reGameStart(){
        this.score = 0;
        this.isOver =false;
        this.player.getComponent(Animation).play()
        this.updateScore();
        this.player.setPosition(-255,0,0)
        this.bg1.setPosition(0,0,0)
        this.bg2.setPosition(716,0,0)
        this.schedule(function(){
            this.score += 1;
            this.updateScore();
            this.createGray();
        },1);
        for (let index = 0; index < this.list.length; index++) {
            this.list[index].destroy()
        }
        this.list.length = 0;
    }
    createGray(){
        let newNode:Node = instantiate(this.gray);
        newNode.parent = this.node;
        let range = 1280 - 120;
        console.log("range*Math.random():"+Math.random());
        let posY = -580 + Math.round(range*Math.random());
        console.log("posY:"+posY);
        newNode.setPosition(newNode.position.x,posY);
        this.list.push(newNode);
    }
    updateScore(){
        this.scoreLabel.string = "score：" + this.score;
    }
    beginContact(){
        console.log("beginContact");
        //停下整个游戏
        this.isOver = true;
        this.unscheduleAllCallbacks();
        this.player.getComponent(Animation).stop()
        globalThis.curScore = this.score;
        if (globalThis.bestScore < globalThis.curScore){
            globalThis.bestScore = globalThis.curScore;
            sys.localStorage.setItem("ac_bestScore",globalThis.bestScore)
        }
        //处理
        UIManager.instance.addView("OverView");
    }
    update(deltaTime: number) {
        if (this.isOver){
            return
        }
        let offX = this.bgSpeed*deltaTime
        let posX = this.bg1.position.x - offX
        if (posX < - 716){
            posX = 716
        }
        this.bg1.setPosition(posX,0,0)
        if (posX > 0){
            this.bg2.setPosition(posX-716,0,0)
        }
        else{
            this.bg2.setPosition(posX+716,0,0)
        }
        if (this.playerDir){
            let offPos = this.playerSpeed*deltaTime;
            let posX = this.player.position.x;
            let posY = this.player.position.y;
            if (this.playerDir == KeyCode.ARROW_UP){
                posY += offPos;
            }else if (this.playerDir == KeyCode.ARROW_DOWN){
                posY -= offPos;
            }else if (this.playerDir == KeyCode.ARROW_LEFT){
                posX -= offPos;
            }else if (this.playerDir == KeyCode.ARROW_RIGHT){
                posX += offPos;
            }
            if (posX>(43-360) && posX<(360-43)&&(posY>(30 - 640)) && posY<(640-30)){
                this.player.setPosition(posX,posY);
            }
            
        }
        let node:Node = null;
        let needPop = false;
        for (let index = 0; index < this.list.length; index++) {
            node = this.list[index]
            node.setPosition(node.position.x - offX,node.position.y); 
            if ((node.position.x - offX) < (-360-70)) {
                needPop = true
            }
        }
        if (needPop){
            this.list[0].destroy()
            this.list.shift();
        }
        
    }
    startDir(event:EventKeyboard){
        if (event.keyCode == KeyCode.ARROW_UP){
            this.playerDir = event.keyCode;
        }else if (event.keyCode == KeyCode.ARROW_DOWN){
            this.playerDir = event.keyCode;
        }else if (event.keyCode == KeyCode.ARROW_LEFT){
            this.playerDir = event.keyCode;
        }else if (event.keyCode == KeyCode.ARROW_RIGHT){
            this.playerDir = event.keyCode;
        }
    }
    stopDir(event:EventKeyboard){
        if (event.keyCode = this.playerDir)
        {
            this.playerDir = null;
        }
    }

}

