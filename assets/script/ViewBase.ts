import { _decorator, Component, Node, AudioSource } from 'cc';
const { ccclass, property } = _decorator;
import {AudioManager} from './AudioManager'
@ccclass('ViewBase')
export class ViewBase extends Component {
    private _openType:string;
    private _closeType:string;
    private _order:number;
    init(){
        
    }
    onLoad(){

    }
    start() {
    }
    onDestory(){

    }

}

