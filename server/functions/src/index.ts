import { initializeApp } from "firebase-admin/app";
import { setGlobalOptions } from "firebase-functions/v2";

initializeApp();
// us-central1 giống server gốc (umgnfrxyuq-uc = us-central1).
setGlobalOptions({ region: "us-central1", maxInstances: 10 });

export {
  joinbossrush, getbossrushpool, startbossrushfight, reportbossrushdamage,
  claimbossrushrewards, updatebossrushplayer, getbossrushprofile, finalizebossrushendedevents,
} from "./bossRush";
export { seedbossrushdummyplayers, removebossrushdummyplayers, finalizebossrushnow } from "./admin";
