import { _decorator, Component, Node, AudioSource, resources, AudioClip } from 'cc';
const { ccclass, property } = _decorator;

@ccclass('AudioManager')
export class AudioManager{
    private static _instance: AudioManager;
    private _audioSource?: AudioSource;

    static get instance () {
        if (this._instance) {
            return this._instance;
        }

        this._instance = new AudioManager();
        return this._instance;
    }


    // 是否播放音乐与音效
    public musicOn:boolean = true;
    public soundOn:boolean = true;

    /**管理器初始化*/
    init (audioSource: AudioSource) {
        this._audioSource = audioSource;
    }
    //切换音乐
    playMusic(name:string){
        if(!this.musicOn){
            return;
        }
        resources.load("music/" + name, AudioClip, (err, audioClip) => {
            if (this._audioSource.playing){
                return;
            }
            this._audioSource.clip = audioClip;
            this._audioSource.play()
        })
    }
    // 关闭音乐
    stopMusic(){
        this._audioSource.stop()
    }
    //播放音效
    playAuidoEff(name:string,volume:number = 1){
        if(!this.soundOn){
            return;
        }
        resources.load("music/" + name, AudioClip, (err, audioClip) => {
            //此处的volume会和 this._audioSource.volume 相乘得出实际声音大小
            this._audioSource.playOneShot(audioClip,volume)
        })
    }
    //设置声音大小
    setAudioVolume(volume:number = 1){
        this._audioSource.volume = volume;
    }
    //获取声音大小
    getAudioVolume():number{
        return this._audioSource.volume;
    }
}

