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
export {
  initpvpprofile, openpvp, findpvpopponents, startpvpbattle, reportpvpbattle, grantpvpadticket, getpvpleaderboard,
  seedpvpdummyplayers, removepvpdummyplayers, removeallpvpplayers,
} from "./pvp";
export {
  createclan, updateclansettings, updateclanannouncement, searchclans, getclandetails, joinopenclan, createclanjoinrequest,
  getclanjoinrequests, acceptclanjoinrequest, denyclanjoinrequest, promoteordemoteclanmember, kickclanmember, leaveclan,
} from "./clan";
export {
  getclanwarstate, recordclanwarcontributions, startclanwarpvpbattle, reportclanwarpvpbattle, claimclanwarmilestone,
  claimclanwarpersonalbundle, claimclanwarclanreward, getclanwarcontributionleaderboard, getclanwarclanleaderboard,
  getclanwarleadershipranking, matchclanwarsweekly, matchclanwarsnow, seedclanwarpoints,
} from "./clanWar";
