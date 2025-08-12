import { _decorator, Component, Node, Slider } from 'cc';
import { AudioManager } from './AudioManager';
import { UIManager } from './UIManager';
const { ccclass, property } = _decorator;

@ccclass('MenuView')
export class MenuView extends Component {
    @property(Slider)
    mySlider:Slider = null;
    SliderNode:Node = null;

    private isShowSilderPar:boolean = false;
    start() {
        AudioManager.instance.playMusic("game_music");
        console.log(AudioManager.instance.getAudioVolume())
        this.mySlider.progress =  AudioManager.instance.getAudioVolume();
        this.SliderNode = this.mySlider.node;
        // console.log(this.isShowSilderPar);
        // console.log(this.SliderNode);
        // 一开始显示为false
        this.SliderNode.active = false;
    }
    // 显示音量
    onShowSlider(){
        // console.log(this.isShowSilderPar);
        this.mySlider.progress =  AudioManager.instance.getAudioVolume();
        this.isShowSilderPar = !this.isShowSilderPar;
        // console.log(this.isShowSilderPar);
        // console.log(this.SliderNode);
        this.SliderNode.active = this.isShowSilderPar;
        this.mySlider.progress =  AudioManager.instance.getAudioVolume();
    }
    // 隐藏音量
    hideSlider(){

    }

    update(deltaTime: number) {
        
    }
    //开始游戏
    onGameStart(){
        UIManager.instance.addView("GameMainView")
        UIManager.instance.closeView("StartMenuView")
    }
    updateVol(slider:Slider){
        if(!slider || !slider.progress){
            // console.log("slider为空"+slider);
            // console.log("slider.progress为空"+slider.progress);
            return;
        }
        // console.log("设置声音大小："+slider.progress);
        AudioManager.instance.setAudioVolume(slider.progress);
    }
}

