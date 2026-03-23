namespace WorldOfTheThreeKingdoms.GameGlobal
{
    public enum FrameFunction
    {
        Browse,
        Jump,
        GetTroopLeader,
        GetOneArchitecture,
        GetCampaignMilitary,
        Architecture_PersonToTroop,
        Architecture_WorkingList,
        PersonTransfer,
        PersonConvene,
        GetConvinceSourcePerson,
        GetConvinceDestinationPerson,
        GetConvinceTargetForAnalysis,  // 新增：用于智能说服的目标分析
        GetDestroyTargetForAnalysis,   // 新增：用于智能破坏的目标分析
        GetInstigateTargetForAnalysis, // 新增：用于智能煽动的目标分析
        GetGossipTargetForAnalysis,    // 新增：用于智能流言的目标分析
        GetEnhanceDiplomaticRelationTargetForAnalysis, // 亲善目标分析
        GetEnhanceDiplomaticRelationCost,              // 亲善代价选择
        GetTruceDiplomaticRelationTargetForAnalysis,   // 停战目标分析
        GetInduceSurrenderTargetForAnalysis,           // 劝降目标分析
        GetInduceSurrenderPerson,                      // 劝降执行人选择
        GetJailBreakTargetForAnalysis,   // 新增：用于智能劫牢的目标分析
        GetAssassinateTargetForAnalysis, // 新增：用于智能暗杀的目标分析
        GetYearlyTalentRecommendation,    // 新增：用于年度人才举荐选择
        GetRewardPerson,
        GetRedeemCaptive,
        GetReleaseCaptive,
        GetStudySkillPerson,
        GetStudyTitlePerson,
        GetStudyTitle,
        GetStudyStuntPerson,
        GetStudyStunt,
        GetTrainingMilitary,
        GetTrainingPerson,
        GetNewMilitaryKind,
        GetRecruitmentMilitary,
        GetRecruitmentPerson,
        GetMergeMilitary,
        GetAutoCampaignMilitaries,
        GetBeMergedMilitaries,
        GetBeDisbandedMilitaries,
        GetLevelUpMilitaries,
        GetLevelUpMiliaryKind,
        GetNewCapital,
        GetFriendlyDiplomaticRelation,
        GetAllyDiplomaticRelation,
        GetAllyDiplomaticRelationTargetForAnalysis,
        GetAllyTargetForAnalysis,                // 结盟目标分析
        GetTruceTargetForAnalysis,           // 停战目标分析
        GetInduceSurrenderExecutor,          // 劝降执行人选择
        GetEnhanceDiplomaticRelation,
        GetEnhanceDiplomaticRelationPerson,
        GetAllyDiplomaticRelationPerson,
        GetDenounceDiplomaticRelation,
        GetTruceDiplomaticRelation,
        GetQuanXiangDiplomaticRelation,//劝降
        GetQuanXiangDiplomaticRelationPerson,
        GetTruceDiplomaticRelationPerson,
        GetAttackDefaultKind,
        GetAttackTargetKind,
        GetCastDefaultKind,
        GetCastTargetKind,
        GetInformationKind,
        GetInformationPerson,
       // GetSpyPerson,
        GetDestroyPerson,
        GetInstigatePerson,
        GetGossipPerson,
        GetSearchPerson,
        GetJailBreakPerson,
        GetAssassinatePerson,
        GetAssassinatePersonTarget,
        GetFacilityToBuild,
        GetFacilityToDemolish,
        Transport,
        GetArchitectureList,
        GetSection,
        GetSectionToDemolish,
        GetSectionAIDetail,
        GetFaction,
        GetDiplomaticRelation,
        GetState,
        GetTroopershipMilitary,
        GetShortestRouteway,
        GetShortestNoWaterRouteway,

        /// <summary>
        /// 宝物-没收
        /// </summary>
        GetConfiscateTreasure,

        /// <summary>
        /// 宝物-授予
        /// </summary>
        GetAwardTreasure,

        /// <summary>
        /// 宝物-授予-人物
        /// </summary>
        GetAwardTreasurePerson,

        /// <summary>
        /// 宝物-出售
        /// </summary>
        GetSellTreasure,
        /// <summary>
        /// 宝物-购买
        /// 🔥 2026-03-03 新增
        /// </summary>
        GetBuyTreasure,

        xuanzemeinv,
        chongxingmeinv,
        MoveFeizi,
        ReleaseFeizi,
        KillPerson,
        KillCaptive,
        MoveCaptive,
        ReleaseSelfPerson,
        PersonManualHire,
        SelectPrince,
        AutoCreatePerson,//自动生成野武将
        GetOfficerType,
        PromoteNvGuan,
       // DengYong,
        AppointMayor, //任命太守
        AppointAdvisor, //任命军师
        DismissOfficer, //遣散
        //AppointOfficer,//任命官员
        GetRecallablePerson,//罢免官员
        GetRecallableTitle,
        GetAppointableTitle,
        GetAppointPerson,
       // MilitaryTransfer, //运输编队
        GetTransferMilitary,//读取运输编队
        GetTransferArchitecture,
        GetInformationToStop,
        SelectLandLink,
        SelectWaterLink,
        SelectMarryablePerson,
        SelectMarryablePerson2,//妾
        SelectMarryTo,
        SelectTrainableChildren,
        SelectTrainPolicy,
        GetGeDiDiplomaticRelation,
        
        // 编辑器相关功能
        GetEditArchitecture,    // 选择要编辑的城池
        GetEditTroop,          // 选择要编辑的部队
        GetEditFaction,        // 选择要编辑的势力
        GetEditPerson,         // 选择要编辑的武将
        GetEditMilitary,       // 选择要编辑的编队
        GetEditTreasure,       // 选择要编辑宝物的武将
        GetEditTitle,          // 选择要编辑称号的武将
        GetEditSkill,          // 选择要编辑特技的武将
        Editor_SelectInfluence // 编辑器选择影响
    }
}

