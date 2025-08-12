import { _decorator,Component, Slider, Node, input, Input, EventKeyboard, KeyCode, Label, PolygonCollider2D, Collider2D, Contact2DType, UI, Animation, System, sys, instantiate, random, debug, Button, Layout, SpriteFrame, ProgressBar, Sprite } from 'cc';
import { UIManager } from './UIManager';
import { AudioManager } from './AudioManager';
import { GameInfoManager } from './GameInfoManager';
const { ccclass, property,integer } = _decorator;

@ccclass('GameBtnControl')
export class GameBtnControl extends Component {
    // 新
        
    @property(Label)
    scoreLabel:Label = null;

    @property(Button)
    pauseBtn:Button = null;
    
    @property(Node)
    pauseBoard:Node = null;

    @property(Button)
    musicBtn:Button = null;

    @property(Button)
    soundBtn:Button = null;

    @property(Button)
    continueBtn:Button = null;

    @property(SpriteFrame)
    musicSpriteFrame1:SpriteFrame = null;

    @property(SpriteFrame)
    musicSpriteFrame2:SpriteFrame = null;

    @property(SpriteFrame)
    soundSpriteFrame1:SpriteFrame = null;
    
    @property(SpriteFrame)
    soundSpriteFrame2:SpriteFrame = null;

    @property(Slider)
    mySlider:Slider = null;

    @property(Label)
    timeLabel:Label = null;

    @property(ProgressBar)
    timerProgressBar:ProgressBar = null;

    
    @property(Label)
    resetLabel:Label = null;

    @property(Node)
    backNode:Node = null;

    @property([SpriteFrame])
    public backSprites:SpriteFrame[] = [];

    backspidx:number = 0;

    // totalTimer:number = 30;
    // currentTimer:number = 30;
    // calTimer:number = 0;
    // public isOver:boolean = false;
    // public isStop:boolean = false;

    // private static _instance: GameBtnControl;

    // static get instance () {
    //     if (this._instance) {
    //         return this._instance;
    //     }

    //     this._instance = new GameBtnControl();
    //     return this._instance;
    // }

    onLoad(){
        globalThis.eventTarget.on("reStart",this.reGameStart,this)
    }
    start() {
        console.log("GameBtnControl:start()");
        this.reGameStart();
    }
    // 更换背景
    changeBackRandom(){
        this.backspidx = (this.backspidx + 1) % this.backSprites.length;
        this.backNode.getComponent(Sprite).spriteFrame = this.backSprites[this.backspidx];
        // if(this.backspidx + 1 < this.backSprites.length){
        //     this.backNode = this.backSprites[this.backspidx + 1];
        //     this.back
        // }else{
        //     this.backNode = this.backSprites[0];
        // }
    }
    reGameStart(){
        // 游戏信息重新
        GameInfoManager.instance.reStart();

        // 声音
        this.scoreLabel.string = "" + GameInfoManager.instance.score;

        // 切换音乐图片显示 打开面板显示即可
        // this.ChangeMusicAndEffectImg();
        
        this.pauseBoard.active = false;

        this.timerProgressBar.progress = 1;
    }
    // 显示音量进度条
    ShowSoundSilderVal(){
        this.mySlider.progress =  AudioManager.instance.getAudioVolume();
    }
    // 切换音乐图片显示
    ChangeMusicAndEffectImg(){
        if(AudioManager.instance.musicOn){
            // 设置图片
            this.musicBtn.getComponent(Button).normalSprite = this.musicSpriteFrame1;
            this.musicBtn.getComponent(Button).pressedSprite = this.musicSpriteFrame1;
            this.musicBtn.getComponent(Button).hoverSprite = this.musicSpriteFrame1;
            this.musicBtn.getComponent(Button).disabledSprite = this.musicSpriteFrame1;
        }else{
            // 设置图片
            this.musicBtn.getComponent(Button).normalSprite = this.musicSpriteFrame2;
            this.musicBtn.getComponent(Button).pressedSprite = this.musicSpriteFrame2;
            this.musicBtn.getComponent(Button).hoverSprite = this.musicSpriteFrame2;
            this.musicBtn.getComponent(Button).disabledSprite = this.musicSpriteFrame2;
        }
        if(AudioManager.instance.soundOn){
            // 设置图片
            this.soundBtn.getComponent(Button).normalSprite = this.soundSpriteFrame1;
            this.soundBtn.getComponent(Button).pressedSprite = this.soundSpriteFrame1;
            this.soundBtn.getComponent(Button).hoverSprite = this.soundSpriteFrame1;
            this.soundBtn.getComponent(Button).disabledSprite = this.soundSpriteFrame1;
        }else{
            // 设置图片
            this.soundBtn.getComponent(Button).normalSprite = this.soundSpriteFrame2;
            this.soundBtn.getComponent(Button).pressedSprite = this.soundSpriteFrame2;
            this.soundBtn.getComponent(Button).hoverSprite = this.soundSpriteFrame2;
            this.soundBtn.getComponent(Button).disabledSprite = this.soundSpriteFrame2;
        }
    }
    // 继续游戏
    onContinue(){
        console.log("onContinue!");
        // this.soundNode.getComponent("SoundControl").playButton();
        this.pauseBoard.active = false;
        GameInfoManager.instance.isStop = false;
    }
    // 暂停，出现面板
    onPause(){
        // 切换音乐图片显示
        this.ChangeMusicAndEffectImg();

        // 音量
        this.ShowSoundSilderVal();

        console.log("OnPause!");
        // this.soundNode.getComponent("SoundControl").playButton();
        this.pauseBoard.active = true;
        // 时间也暂停
        GameInfoManager.instance.isStop = true;
        // console.log("GameInfoManager.instance.isStop:"+GameInfoManager.instance.isStop);
    }
    // 播放背景音乐
    onMusic(){
        // this.soundNode.getComponent("SoundControl").playButton();
        AudioManager.instance.musicOn = !AudioManager.instance.musicOn;
        if(AudioManager.instance.musicOn){
            // 关闭或者开启音乐
            AudioManager.instance.playMusic("game_music");
        }else{
            // 关闭或者开启音乐
            AudioManager.instance.stopMusic();
        }
        this.ChangeMusicAndEffectImg();
    }
    // 播放音效
    onSound(){
        AudioManager.instance.soundOn = !AudioManager.instance.soundOn;
        this.ChangeMusicAndEffectImg();
    }
    // 改变音量
    updateVol(slider:Slider){
        if(!slider || !slider.progress){
            // console.log("slider为空"+slider);
            // console.log("slider.progress为空"+slider.progress);
            return;
        }
        // console.log("设置声音大小："+slider.progress);
        AudioManager.instance.setAudioVolume(slider.progress);
    }
    // 回到主界面
    backMainMenu(){
        UIManager.instance.addView("StartMenuView")
        UIManager.instance.closeView("GameMainView")
    }
    // 切换结束界面
    gameOverChange(){
        globalThis.curScore = GameInfoManager.instance.score;
        UIManager.instance.addView("OverMenuView")
    }
    update(deltaTime: number) {
        if(GameInfoManager.instance.isStop){
            return;
        }
        if(GameInfoManager.instance.isOver){
            this.gameOverChange();
            return;
        }
        GameInfoManager.instance.calTimer += deltaTime;
        if(GameInfoManager.instance.calTimer >= 1.0){
            //console.log(GameInfoManager.instance.calTimer);
            GameInfoManager.instance.calTimer = 0;
            GameInfoManager.instance.currentTimer--;
            this.timerProgressBar.progress = (GameInfoManager.instance.currentTimer / GameInfoManager.instance.totalTimer);
            // 更新label
            this.timeLabel.string = "" + GameInfoManager.instance.currentTimer;

            // 游戏结束
            if(GameInfoManager.instance.currentTimer <= 0){
                GameInfoManager.instance.isOver = true;
            }
        }
        // 更新label UI
        this.timerProgressBar.progress = (GameInfoManager.instance.currentTimer / GameInfoManager.instance.totalTimer);
        this.timeLabel.string = "" + GameInfoManager.instance.currentTimer; 
        this.resetLabel.string = "" +GameInfoManager.instance.resetNum;
    }

}

