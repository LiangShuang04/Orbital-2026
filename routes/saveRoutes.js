import { Router } from "express";
import auth from "#middleware/auth";
import { createSaveProfile, getSaveProfile, updateSaveProfile } from "#controllers/saveController";

const router = Router();

router.use(auth);
router.route("/").post(createSaveProfile).get(getSaveProfile).put(updateSaveProfile);

export default router;
