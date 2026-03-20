er encounter enc_12 (complete, restored=False, next_tick=16261, map=Town, time=1000).
[03:15:04 DEBUG The Living Valley] Autonomy: [HANDOFF] Martin starting handoff: TilePoint=(87,52), controller=null, followSchedule=True, time=1000, map=Town.
[03:15:04 TRACE The Living Valley] Autonomy: queued Martin for vanilla schedule resume after encounter enc_12 (complete, restored=False, next_tick=16261, map=Town, time=1000).
[03:15:04 DEBUG The Living Valley] Autonomy: Player2 encounter enc_12 MorrisTod->Martin completed (outcome=friendly).
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] MorrisTod starting rebind at TilePoint=(89,53), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1000.
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] MorrisTod cleared schedule, calling TryLoadSchedule().
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] MorrisTod TryLoadSchedule returned=True, schedule_count=6, first_keys=610,900,930,2300,2510.
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] MorrisTod current_time=1000, entries_before_current=610:Town,900:Town,930:JojaMart.
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] MorrisTod encounter=enc_12 from=Town to=JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30) arrival_resolved=True active_target_location=JojaMart active_target_tile=(24,26) time=1000.
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] MorrisTod reset complete: lastAttemptedSchedule=1000, previousEndPoint=(95,50), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=930, next_schedule_time=2300, active_target_location=JojaMart, active_target_tile=(24,26), active_facing=3, active_behavior=none, fallback_used=True.
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] Martin starting rebind at TilePoint=(87,52), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1000.
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] Martin cleared schedule, calling TryLoadSchedule().
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] Martin TryLoadSchedule returned=True, schedule_count=4, first_keys=800,2200,2400,2410.
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] Martin current_time=1000, entries_before_current=800:JojaMart.
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Martin encounter=enc_12 from=Custom_Martin_WarpRoom to=BusStop transition_tile=(0,3) approach_tile=(0,3) arrival_tile=(22,9) arrival_resolved=True active_target_location=JojaMart active_target_tile=(9,25) time=1000.
[03:15:04 DEBUG The Living Valley] Autonomy: [REBIND] Martin reset complete: lastAttemptedSchedule=1000, previousEndPoint=(0,3), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=800, next_schedule_time=2200, active_target_location=JojaMart, active_target_tile=(9,25), active_facing=1, active_behavior=Martin_Idle, fallback_used=True.
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(transition_ready)] Martin encounter=enc_12 map=Custom_Martin_WarpRoom tile=(1,3) transition_tile=(0,3) approach_tile=(0,3).
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warping)] Martin encounter=enc_12 from=Custom_Martin_WarpRoom to=BusStop transition_tile=(0,3) approach_tile=(0,3) arrival_tile=(22,9).
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(22,9) target_leg=Custom_Martin_WarpRoom->BusStop transition_tile=(0,3) approach_tile=(0,3) arrival_tile=(22,9).
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warped)] Martin encounter=enc_12 reached BusStop from Custom_Martin_WarpRoom.
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Martin encounter=enc_12 from=BusStop to=Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54) arrival_resolved=True active_target_location=JojaMart active_target_tile=(9,25) time=1000.
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,74) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(22,10) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] MorrisTod encounter=enc_12 map=Town tile=(90,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,75) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(23,10) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] MorrisTod encounter=enc_12 map=Town tile=(91,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:05 DEBUG The Living Valley] Autonomy: [ARRIVAL] Abigail active-slot handoff at tile (39,5) in SeedShop (active_schedule_time=900, active_facing=0, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(39,6), facing=1, time=1000).
[03:15:05 DEBUG The Living Valley] Autonomy: returned Abigail to active-slot schedule action after encounter enc_11 (complete, restored=False, attempts=1, active_schedule_time=900, next_schedule_time=1030, active_target_location=SeedShop, active_target_tile=(39,5), active_facing=0, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(39,6), previousEndPoint=(39,5), lastAttemptedSchedule=1000, map=SeedShop, time=1000).
[03:15:05 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_11 tick=1: controller=PathFindController, isMoving=True, TilePoint=(39,6), moved_from_initial=yes, previousEndPoint=(39,5), followSchedule=True.
[03:15:05 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,76) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:05 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(24,10) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:05 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_11 tick=2: controller=PathFindController, isMoving=True, TilePoint=(39,6), moved_from_initial=yes, previousEndPoint=(39,5), followSchedule=True.
[03:15:05 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] MorrisTod encounter=enc_12 map=Town tile=(92,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:05 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_11 tick=3: controller=PathFindController, isMoving=True, TilePoint=(39,6), moved_from_initial=yes, previousEndPoint=(39,5), followSchedule=True.
[03:15:05 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_11 tick=4: controller=PathFindController, isMoving=True, TilePoint=(40,6), moved_from_initial=yes, previousEndPoint=(39,5), followSchedule=True.
[03:15:05 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,77) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:05 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,10) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:05 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_11 tick=5: controller=PathFindController, isMoving=True, TilePoint=(40,6), moved_from_initial=yes, previousEndPoint=(39,5), followSchedule=True.
[03:15:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] MorrisTod encounter=enc_12 map=Town tile=(93,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,78) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,11) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] MorrisTod encounter=enc_12 map=Town tile=(94,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,79) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,12) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] MorrisTod encounter=enc_12 map=Town tile=(95,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,80) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,13) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] MorrisTod encounter=enc_12 map=Town tile=(95,52) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:08 TRACE The Living Valley] Autonomy: Lewis found target Daulton but out of talk range (dist=16.0).
[03:15:08 TRACE The Living Valley] Autonomy: Robin found target Gus but out of talk range (dist=52.0).
[03:15:08 TRACE The Living Valley] Autonomy: Alex found target Daulton but out of talk range (dist=26.0).
[03:15:08 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=18.0).
[03:15:08 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:15:08 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:15:08 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=52.0).
[03:15:08 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=18.0).
[03:15:08 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=20.0).
[03:15:08 TRACE The Living Valley] Autonomy: Kent found target Penny but out of talk range (dist=15.0).
[03:15:08 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=20.0).
[03:15:08 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:08 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:08 TRACE The Living Valley] Autonomy: Penny found target Kent but out of talk range (dist=15.0).
[03:15:08 TRACE The Living Valley] Autonomy: Sam found target Jodi but out of talk range (dist=16.0).
[03:15:08 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:08 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=13.0).
[03:15:08 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=182.0).
[03:15:08 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:15:08 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:08 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=38.0).
[03:15:08 TRACE The Living Valley] Autonomy: Julia found target Arthur but out of talk range (dist=56.0).
[03:15:08 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:08 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:08 TRACE The Living Valley] Autonomy: Victor found target MorrisTod but out of talk range (dist=18.0).
[03:15:08 TRACE The Living Valley] Autonomy: Daulton found target Lewis but out of talk range (dist=16.0).
[03:15:08 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=26.0).
[03:15:08 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,81) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,14) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] MorrisTod encounter=enc_12 map=Town tile=(95,51) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(transition_ready)] MorrisTod encounter=enc_12 map=Town tile=(95,51) transition_tile=(95,50) approach_tile=(95,50).
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warping)] MorrisTod encounter=enc_12 from=Town to=JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] MorrisTod encounter=enc_12 map=JojaMart tile=(13,29) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warped)] MorrisTod encounter=enc_12 reached JojaMart from Town.
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(target_map)] MorrisTod encounter=enc_12 reached target map JojaMart; switching to active-slot target fallback.
[03:15:08 DEBUG The Living Valley] Autonomy: [FORCE_PATH] MorrisTod forced same-map active-slot path after encounter enc_12 (active_schedule_time=930, next_schedule_time=2300, location=JojaMart, tile=(24,26), time=1010).
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,82) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,15) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:09 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,83) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:09 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,16) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:09 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(54,84) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:09 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,17) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,84) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,18) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:10 DEBUG The Living Valley] Autonomy: Gunther->GuntherSilvian encounter approved! block=ReturnHome location=ArchaeologyHouse.
[03:15:10 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian blocked by wall (no line of sight).
[03:15:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,85) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,19) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,86) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,20) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,87) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,21) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,88) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(25,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:13 TRACE The Living Valley] Autonomy: Lewis found target Daulton but out of talk range (dist=8.0).
[03:15:13 TRACE The Living Valley] Autonomy: Robin found target Gus but out of talk range (dist=43.0).
[03:15:13 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=26.0).
[03:15:13 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=13.0).
[03:15:13 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=18.0).
[03:15:13 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=18.0).
[03:15:13 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=43.0).
[03:15:13 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=13.0).
[03:15:13 TRACE The Living Valley] Autonomy: Jodi found target Penny but out of talk range (dist=35.0).
[03:15:13 TRACE The Living Valley] Autonomy: Kent found target Penny but out of talk range (dist=15.0).
[03:15:13 TRACE The Living Valley] Autonomy: Marnie found target Andy but out of talk range (dist=66.0).
[03:15:13 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:13 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:13 TRACE The Living Valley] Autonomy: Penny found target Kent but out of talk range (dist=15.0).
[03:15:13 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=44.0).
[03:15:13 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:13 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=15.0).
[03:15:13 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=44.0).
[03:15:13 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=23.0).
[03:15:13 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=192.0).
[03:15:13 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:15:13 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:13 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=38.0).
[03:15:13 TRACE The Living Valley] Autonomy: Julia found target Arthur but out of talk range (dist=56.0).
[03:15:13 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:13 TRACE The Living Valley] Autonomy: Andy found target Marnie but out of talk range (dist=66.0).
[03:15:13 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:13 TRACE The Living Valley] Autonomy: Victor found target Alex but out of talk range (dist=35.0).
[03:15:13 TRACE The Living Valley] Autonomy: Daulton found target Lewis but out of talk range (dist=8.0).
[03:15:13 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=36.0).
[03:15:13 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,89) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(26,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,90) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(27,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,91) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(28,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,92) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(29,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,93) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(30,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,94) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(31,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,95) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(32,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,96) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(33,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:17 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,97) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:17 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(34,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:18 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,98) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:18 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(35,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:18 TRACE The Living Valley] Autonomy: Lewis found target Daulton but out of talk range (dist=17.0).
[03:15:18 TRACE The Living Valley] Autonomy: Robin found target Gus but out of talk range (dist=48.0).
[03:15:18 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=70.0).
[03:15:18 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=26.0).
[03:15:18 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=21.0).
[03:15:18 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=22.0).
[03:15:18 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=18.0).
[03:15:18 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=18.0).
[03:15:18 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=48.0).
[03:15:18 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=22.0).
[03:15:18 TRACE The Living Valley] Autonomy: Jodi found target Penny but out of talk range (dist=40.0).
[03:15:18 TRACE The Living Valley] Autonomy: Kent found target Penny but out of talk range (dist=15.0).
[03:15:18 TRACE The Living Valley] Autonomy: Marnie found target Andy but out of talk range (dist=74.0).
[03:15:18 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:18 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:18 TRACE The Living Valley] Autonomy: Penny found target Kent but out of talk range (dist=15.0).
[03:15:18 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=53.0).
[03:15:18 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:18 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=18.0).
[03:15:18 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=53.0).
[03:15:18 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=32.0).
[03:15:18 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=201.0).
[03:15:18 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:15:18 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:18 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=38.0).
[03:15:18 TRACE The Living Valley] Autonomy: Julia found target Arthur but out of talk range (dist=56.0).
[03:15:18 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:18 TRACE The Living Valley] Autonomy: Andy found target Marnie but out of talk range (dist=74.0).
[03:15:18 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:18 TRACE The Living Valley] Autonomy: Victor found target Alex but out of talk range (dist=44.0).
[03:15:18 TRACE The Living Valley] Autonomy: Daulton found target Lewis but out of talk range (dist=17.0).
[03:15:18 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=45.0).
[03:15:18 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:18 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,99) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:18 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(36,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:19 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,100) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:19 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(37,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:19 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,101) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:19 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(38,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:20 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,102) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:20 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(39,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:20 DEBUG The Living Valley] Autonomy: [ARRIVAL] MorrisTod active-slot handoff at tile (24,26) in JojaMart (active_schedule_time=930, active_facing=3, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(23,26), facing=1, time=1020).
[03:15:20 DEBUG The Living Valley] Autonomy: returned MorrisTod to active-slot schedule action after encounter enc_12 (complete, restored=False, attempts=1, active_schedule_time=930, next_schedule_time=2300, active_target_location=JojaMart, active_target_tile=(24,26), active_facing=3, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(23,26), previousEndPoint=(24,26), lastAttemptedSchedule=1020, map=JojaMart, time=1020).
[03:15:20 DEBUG The Living Valley] Autonomy: [MONITOR] MorrisTod encounter=enc_12 tick=1: controller=PathFindController, isMoving=True, TilePoint=(23,26), moved_from_initial=yes, previousEndPoint=(24,26), followSchedule=True.
[03:15:20 DEBUG The Living Valley] Autonomy: [REBIND] Arthur reset complete: lastAttemptedSchedule=1030, previousEndPoint=(19,9), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1030, next_schedule_time=1500, active_target_location=Downhill, active_target_tile=(19,9), active_facing=1, active_behavior=clint_hammer, fallback_used=False.
[03:15:20 DEBUG The Living Valley] Autonomy: returned Arthur to vanilla schedule after encounter enc_7 (complete, restored=False, attempts=2, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1030, active_schedule_time=1030, next_schedule_time=1500, active_target_location=Downhill, active_target_tile=(19,9), fallback_used=False, resumed=true, method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(35,39), previousEndPoint=(19,9), lastAttemptedSchedule=1030, map=Downhill, time=1030).
[03:15:20 DEBUG The Living Valley] Autonomy: [MONITOR] MorrisTod encounter=enc_12 tick=2: controller=PathFindController, isMoving=True, TilePoint=(23,26), moved_from_initial=yes, previousEndPoint=(24,26), followSchedule=True.
[03:15:20 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_7 tick=1: controller=PathFindController, isMoving=True, TilePoint=(35,39), moved_from_initial=no, previousEndPoint=(19,9), followSchedule=True.
[03:15:20 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,103) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:20 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(40,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:20 DEBUG The Living Valley] Autonomy: [MONITOR] MorrisTod encounter=enc_12 tick=3: controller=PathFindController, isMoving=True, TilePoint=(23,26), moved_from_initial=yes, previousEndPoint=(24,26), followSchedule=True.
[03:15:20 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_7 tick=2: controller=PathFindController, isMoving=True, TilePoint=(35,40), moved_from_initial=yes, previousEndPoint=(19,9), followSchedule=True.
[03:15:20 DEBUG The Living Valley] Autonomy: [MONITOR] MorrisTod encounter=enc_12 tick=4: controller=PathFindController, isMoving=True, TilePoint=(24,26), moved_from_initial=yes, previousEndPoint=(24,26), followSchedule=True.
[03:15:20 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_7 tick=3: controller=PathFindController, isMoving=True, TilePoint=(35,40), moved_from_initial=yes, previousEndPoint=(19,9), followSchedule=True.
[03:15:21 DEBUG The Living Valley] Autonomy: [MONITOR] MorrisTod encounter=enc_12 tick=5: controller=PathFindController, isMoving=True, TilePoint=(24,26), moved_from_initial=yes, previousEndPoint=(24,26), followSchedule=True.
[03:15:21 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_7 tick=4: controller=PathFindController, isMoving=True, TilePoint=(34,40), moved_from_initial=yes, previousEndPoint=(19,9), followSchedule=True.
[03:15:21 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_7 tick=5: controller=PathFindController, isMoving=True, TilePoint=(34,40), moved_from_initial=yes, previousEndPoint=(19,9), followSchedule=True.
[03:15:21 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,104) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:21 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(41,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:21 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,105) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:21 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(42,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,106) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=BusStop tile=(43,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(transition_ready)] Martin encounter=enc_12 map=BusStop tile=(43,22) transition_tile=(44,22) approach_tile=(44,22).
[03:15:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warping)] Martin encounter=enc_12 from=BusStop to=Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(0,54) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:15:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warped)] Martin encounter=enc_12 reached Town from BusStop.
[03:15:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Martin encounter=enc_12 from=Town to=JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30) arrival_resolved=True active_target_location=JojaMart active_target_tile=(9,25) time=1030.
[03:15:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(1,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,107) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:23 TRACE The Living Valley] Autonomy: Lewis found target Daulton but out of talk range (dist=26.0).
[03:15:23 TRACE The Living Valley] Autonomy: Robin found target Gus but out of talk range (dist=43.0).
[03:15:23 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=79.0).
[03:15:23 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=26.0).
[03:15:23 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=20.0).
[03:15:23 TRACE The Living Valley] Autonomy: Emily found target Penny but out of talk range (dist=18.0).
[03:15:23 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=18.0).
[03:15:23 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=18.0).
[03:15:23 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=43.0).
[03:15:23 TRACE The Living Valley] Autonomy: Jodi found target Emily but out of talk range (dist=27.0).
[03:15:23 TRACE The Living Valley] Autonomy: Kent found target Penny but out of talk range (dist=12.0).
[03:15:23 TRACE The Living Valley] Autonomy: Marnie found target Andy but out of talk range (dist=81.0).
[03:15:23 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:23 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:23 TRACE The Living Valley] Autonomy: Penny found target Kent but out of talk range (dist=12.0).
[03:15:23 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=62.0).
[03:15:23 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:23 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=28.0).
[03:15:23 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=62.0).
[03:15:23 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=211.0).
[03:15:23 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:15:23 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:23 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=35.0).
[03:15:23 TRACE The Living Valley] Autonomy: Julia found target Arthur but out of talk range (dist=59.0).
[03:15:23 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:23 TRACE The Living Valley] Autonomy: Andy found target Marnie but out of talk range (dist=81.0).
[03:15:23 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:23 TRACE The Living Valley] Autonomy: Victor found target Alex but out of talk range (dist=53.0).
[03:15:23 TRACE The Living Valley] Autonomy: Daulton found target Kent but out of talk range (dist=23.0).
[03:15:23 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=54.0).
[03:15:23 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:23 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(2,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:23 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,108) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:23 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(3,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:24 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(55,109) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:24 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(4,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:24 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(56,109) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:25 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(5,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:25 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(57,109) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:26 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(6,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:26 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(57,110) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:26 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(7,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:26 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(57,111) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:27 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(8,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:27 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(57,112) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:27 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(9,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:27 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(57,113) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:28 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(10,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:28 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(57,114) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:28 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=26.0).
[03:15:28 TRACE The Living Valley] Autonomy: Robin found target Alex but out of talk range (dist=39.0).
[03:15:28 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=89.0).
[03:15:28 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=26.0).
[03:15:28 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=20.0).
[03:15:28 TRACE The Living Valley] Autonomy: Emily found target Jodi but out of talk range (dist=21.0).
[03:15:28 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=18.0).
[03:15:28 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=18.0).
[03:15:28 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=48.0).
[03:15:28 TRACE The Living Valley] Autonomy: Jodi found target Emily but out of talk range (dist=21.0).
[03:15:28 TRACE The Living Valley] Autonomy: Kent found target Penny but out of talk range (dist=5.0).
[03:15:28 TRACE The Living Valley] Autonomy: Leah found target Marnie but out of talk range (dist=25.0).
[03:15:28 TRACE The Living Valley] Autonomy: Marnie found target Leah but out of talk range (dist=25.0).
[03:15:28 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:28 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:28 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=37.0).
[03:15:28 TRACE The Living Valley] Autonomy: Penny found target Kent but out of talk range (dist=5.0).
[03:15:28 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=71.0).
[03:15:28 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:28 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=37.0).
[03:15:28 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=71.0).
[03:15:28 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=220.0).
[03:15:28 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:15:28 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:28 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=26.0).
[03:15:28 TRACE The Living Valley] Autonomy: Julia found target Arthur but out of talk range (dist=68.0).
[03:15:28 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:28 TRACE The Living Valley] Autonomy: Andy found target Leah but out of talk range (dist=58.0).
[03:15:28 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:28 TRACE The Living Valley] Autonomy: Martin found target Jodi but out of talk range (dist=27.0).
[03:15:28 TRACE The Living Valley] Autonomy: Victor found target Alex but out of talk range (dist=62.0).
[03:15:28 TRACE The Living Valley] Autonomy: Daulton found target Lewis but out of talk range (dist=31.0).
[03:15:28 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=64.0).
[03:15:28 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:28 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(11,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Town tile=(57,115) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(transition_ready)] Daulton encounter=enc_9 map=Town tile=(57,115) transition_tile=(57,116) approach_tile=(57,116).
[03:15:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warping)] Daulton encounter=enc_9 from=Town to=Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Daulton encounter=enc_9 map=Beach tile=(38,0) target_leg=Town->Beach transition_tile=(57,116) approach_tile=(57,116) arrival_tile=(38,0).
[03:15:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warped)] Daulton encounter=enc_9 reached Beach from Town.
[03:15:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(target_map)] Daulton encounter=enc_9 reached target map Beach; switching to active-slot target fallback.
[03:15:29 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Daulton forced same-map active-slot path after encounter enc_9 (active_schedule_time=900, next_schedule_time=1200, location=Beach, tile=(44,35), time=1040).
[03:15:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(12,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(13,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:30 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(14,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:31 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(15,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:31 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(16,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:32 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(17,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:32 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(18,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:33 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(19,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:33 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=23.0).
[03:15:33 TRACE The Living Valley] Autonomy: Robin found target Alex but out of talk range (dist=32.0).
[03:15:33 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=98.0).
[03:15:33 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=23.0).
[03:15:33 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=20.0).
[03:15:33 TRACE The Living Valley] Autonomy: Emily found target Jodi but out of talk range (dist=15.0).
[03:15:33 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=18.0).
[03:15:33 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=18.0).
[03:15:33 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=45.0).
[03:15:33 TRACE The Living Valley] Autonomy: Jodi found target Martin but out of talk range (dist=14.0).
[03:15:33 TRACE The Living Valley] Autonomy: Kent found target Penny but out of talk range (dist=14.0).
[03:15:33 TRACE The Living Valley] Autonomy: Leah found target Andy but out of talk range (dist=65.0).
[03:15:33 TRACE The Living Valley] Autonomy: Marnie found target Kent but out of talk range (dist=24.0).
[03:15:33 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:33 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:33 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=46.0).
[03:15:33 TRACE The Living Valley] Autonomy: Penny found target Kent but out of talk range (dist=14.0).
[03:15:33 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=81.0).
[03:15:33 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:33 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=46.0).
[03:15:33 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=81.0).
[03:15:33 TRACE The Living Valley] Autonomy: Willy found target Daulton but out of talk range (dist=10.0).
[03:15:33 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=229.0).
[03:15:33 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:15:33 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:33 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=16.0).
[03:15:33 TRACE The Living Valley] Autonomy: Julia found target Arthur but out of talk range (dist=78.0).
[03:15:33 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:33 TRACE The Living Valley] Autonomy: Andy found target Leah but out of talk range (dist=65.0).
[03:15:33 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:33 TRACE The Living Valley] Autonomy: Martin found target Jodi but out of talk range (dist=14.0).
[03:15:33 TRACE The Living Valley] Autonomy: Daulton found target Willy but out of talk range (dist=10.0).
[03:15:33 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=73.0).
[03:15:33 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:33 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(20,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:34 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(21,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:34 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(22,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(23,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:35 TRACE Farm Type Manager (FTM)] Spawned 1 objects. Time: 1050.
[03:15:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(24,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:36 DEBUG The Living Valley] Ambient NPC multi-turn conversation triggered: Willy's Helper -> Jodi turns=2 beat=community.
[03:15:36 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(25,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:37 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(26,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:37 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(27,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:38 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(28,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:38 TRACE The Living Valley] Player2 stream line: {"message":"\u003cdaulton\u003e That ruckus got folks knotted; some trust in town’s leaders is definitely strained.","npc_id":"05f5d77b-00fb-49c3-b8b0-423d79e2f07f"}
[03:15:38 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=23.0).
[03:15:38 TRACE The Living Valley] Autonomy: Robin found target Gus but out of talk range (dist=38.0).
[03:15:38 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=107.0).
[03:15:38 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=23.0).
[03:15:38 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=20.0).
[03:15:38 TRACE The Living Valley] Autonomy: Emily found target Jodi but out of talk range (dist=7.0).
[03:15:38 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=13.0).
[03:15:38 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=13.0).
[03:15:38 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=38.0).
[03:15:38 TRACE The Living Valley] Autonomy: Jodi found target Martin but out of talk range (dist=6.0).
[03:15:38 TRACE The Living Valley] Autonomy: Kent found target Marnie but out of talk range (dist=5.0).
[03:15:38 TRACE The Living Valley] Autonomy: Leah found target Andy but out of talk range (dist=74.0).
[03:15:38 TRACE The Living Valley] Autonomy: Marnie found target Kent but out of talk range (dist=5.0).
[03:15:38 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:38 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:38 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=56.0).
[03:15:38 TRACE The Living Valley] Autonomy: Penny found target Kent but out of talk range (dist=24.0).
[03:15:38 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=90.0).
[03:15:38 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:38 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=56.0).
[03:15:38 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=90.0).
[03:15:38 TRACE The Living Valley] Autonomy: Willy found target Daulton but out of talk range (dist=16.0).
[03:15:38 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=239.0).
[03:15:38 TRACE The Living Valley] Autonomy: Chloe found target Arthur but out of talk range (dist=7.0).
[03:15:38 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:38 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=7.0).
[03:15:38 TRACE The Living Valley] Autonomy: Julia found target Arthur but out of talk range (dist=87.0).
[03:15:38 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:38 TRACE The Living Valley] Autonomy: Andy found target Leah but out of talk range (dist=74.0).
[03:15:38 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:38 TRACE The Living Valley] Autonomy: Martin found target Jodi but out of talk range (dist=6.0).
[03:15:38 TRACE The Living Valley] Autonomy: Daulton found target Willy but out of talk range (dist=16.0).
[03:15:38 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=82.0).
[03:15:38 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:38 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(29,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:39 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:15:39 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(29,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:39 TRACE The Living Valley] Autonomy: Kent->Marnie skipped by 50% encounter gate (block=BaseAnchor).
[03:15:39 DEBUG The Living Valley] Autonomy: Marnie->Kent encounter approved! block=BaseAnchor location=Town.
[03:15:39 DEBUG The Living Valley] Autonomy: Marnie->Kent staged successfully, starting conversation.
[03:15:39 DEBUG The Living Valley] Autonomy: Marnie->Kent Player2 encounter conversation launched (turns=4, continuation=False).
[03:15:39 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(30,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:39 TRACE The Living Valley] Player2 stream line: {"message":"\u003cJodi\u003e The town council’s meeting’s attendance dropped, so trust’s tighter than ever.","npc_id":"2cf2cdd3-dd20-4835-855f-60f15a291671"}
[03:15:40 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(31,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:40 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:40 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(32,53) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:40 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:15:41 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:41 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(32,54) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:41 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:41 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(32,55) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:42 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:42 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=2s)
[03:15:42 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(32,56) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:42 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:42 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_12 map=Town tile=(32,57) target_leg=Town->JojaMart transition_tile=(95,50) approach_tile=(95,50) arrival_tile=(13,30).
[03:15:43 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:43 DEBUG The Living Valley] Autonomy: Emily->Martin encounter approved! block=BaseAnchor location=Town.
[03:15:43 DEBUG The Living Valley] Autonomy: Emily->Martin staged successfully, starting conversation.
[03:15:43 DEBUG The Living Valley] Autonomy: Emily->Martin Player2 encounter conversation launched (turns=4, continuation=False).
[03:15:43 TRACE The Living Valley] Player2 stream line: {"message":"\u003cKent\u003e Nah, missed it—was sorting papers at the bus stop.","npc_id":"2a9b5bb7-84bd-4089-9f6a-f39c5019503e"}
[03:15:43 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:43 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=23.0).
[03:15:43 TRACE The Living Valley] Autonomy: Robin found target Jodi but out of talk range (dist=30.0).
[03:15:43 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=117.0).
[03:15:43 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=23.0).
[03:15:43 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=20.0).
[03:15:43 TRACE The Living Valley] Autonomy: Evelyn->George skipped by 50% encounter gate (block=BaseAnchor).
[03:15:43 TRACE The Living Valley] Autonomy: George->Evelyn skipped by 50% encounter gate (block=BaseAnchor).
[03:15:43 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=36.0).
[03:15:43 TRACE The Living Valley] Autonomy: Jodi found target Robin but out of talk range (dist=30.0).
[03:15:43 TRACE The Living Valley] Autonomy: Leah found target Andy but out of talk range (dist=85.0).
[03:15:43 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:43 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:43 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=65.0).
[03:15:43 TRACE The Living Valley] Autonomy: Penny found target Willy but out of talk range (dist=31.0).
[03:15:43 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=100.0).
[03:15:43 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:43 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=65.0).
[03:15:43 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=100.0).
[03:15:43 TRACE The Living Valley] Autonomy: Willy found target Lewis but out of talk range (dist=33.0).
[03:15:43 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=248.0).
[03:15:43 TRACE The Living Valley] Autonomy: Chloe found target Arthur but out of talk range (dist=10.0).
[03:15:43 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:43 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=10.0).
[03:15:43 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=94.0).
[03:15:43 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:43 TRACE The Living Valley] Autonomy: Andy found target Leah but out of talk range (dist=85.0).
[03:15:43 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=13.0).
[03:15:43 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=92.0).
[03:15:43 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:43 DEBUG The Living Valley] Autonomy: George->Evelyn staged successfully, starting conversation.
[03:15:43 DEBUG The Living Valley] Autonomy: George->Evelyn Player2 encounter conversation launched (turns=4, continuation=False).
[03:15:44 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:44 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:44 TRACE The Living Valley] Player2 stream line: {"command":[{"arguments":"{\"confidence\":0.8,\"target_group\":\"council\",\"target_npc\":\"2cf2cdd3-dd20-4835-855f-60f15a291671\",\"topic\":\"Council meeting attendance dropped, trust is tighter than ever.\"}","name":"spread_rumor"}],"message":"\u003cdaulton\u003e Sharing Jodi's concern about the council meeting attendance.","npc_id":"05f5d77b-00fb-49c3-b8b0-423d79e2f07f"}
[03:15:44 DEBUG The Living Valley] Ambient NPC multi-turn completed: Willy's Helper->Jodi turns=2/2 duration_ms=8592 retries=0 timeouts=0. beat=community
[03:15:44 TRACE The Living Valley] Ambient transcript T1 Willy's Helper->Jodi: That ruckus got folks knotted; some trust in town’s leaders is definitely strained.
[03:15:44 TRACE The Living Valley] Ambient transcript T2 Jodi->Willy's Helper: The town council’s meeting’s attendance dropped, so trust’s tighter than ever.
[03:15:44 TRACE The Living Valley] Ambient dialogue kept off-bubble for Jodi: The town council’s meeting’s attendance dropped, so trust’s tighter than ever.
[03:15:44 DEBUG The Living Valley] Applied NPC command lane=chat: record_memory_fact -> outcome memory:daulton:event (intent=synth_ambient_mem_121_05f5d77b_00fb_49c3_b8b0_423d79e2f07f_05f5d77b_00fb_49c3_b8b0_423d79e2f07f_2cf2cdd3_dd20_4835_855f_)
[03:15:44 DEBUG The Living Valley] Applied NPC command lane=chat: record_memory_fact -> outcome memory:Jodi:event (intent=synth_ambient_mem_121_2cf2cdd3_dd20_4835_855f_60f15a291671_05f5d77b_00fb_49c3_b8b0_423d79e2f07f_2cf2cdd3_dd20_4835_855f_)
[03:15:44 DEBUG The Living Valley] Ambient fallback memory persisted for Willy's Helper<->Jodi: topic='The town council’s meeting’s attendance dropped, so trust’s tighter than ever.'.
[03:15:44 TRACE The Living Valley] Queued ambient overhear gossip cue for player dialogue: Willy's Helper->Jodi.
[03:15:44 TRACE The Living Valley] Ambient command unlocks day 121: adjust_town_sentiment | events=4 public=2 market=0 scarcity=0 oversupply=0 anomaly=False
[03:15:44 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:44 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:44 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:44 TRACE The Living Valley] Player2 stream line: {"message":"\u003cEmily\u003e Did you catch Andy and Susan's live show by the square? It was electrifying!","npc_id":"93aedf85-550d-4129-921a-0c89057e1104"}
[03:15:45 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:45 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:45 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:45 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:15:45 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:45 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:45 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:45 TRACE The Living Valley] Player2 stream line: {"message":"\u003cGeorge\u003e That Andy and Susan's stunt left a mess on Main, eh?","npc_id":"e83bc960-3f11-49d0-8b92-f142f63f8305"}
[03:15:46 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:46 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:46 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:46 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:15:46 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:46 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:46 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:47 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:47 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:47 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:47 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:47 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:47 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:47 TRACE The Living Valley] Player2 stream line: {"message":"\u003cKent\u003e Well, back to it—got to finish those reports before lunch.","npc_id":"2a9b5bb7-84bd-4089-9f6a-f39c5019503e"}
[03:15:47 DEBUG The Living Valley] Encounter conversation completed: Marnie->Kent enc=enc_13 turns=4/4 duration_ms=8200.
[03:15:47 TRACE The Living Valley] Encounter transcript T1 Marnie->Kent: Did you catch Andy and Susan’s stunt at the square this morning?
[03:15:47 TRACE The Living Valley] Encounter transcript T2 Kent->Marnie: Nah, missed it—was sorting papers at the bus stop.
[03:15:47 TRACE The Living Valley] Encounter transcript T3 Marnie->Kent: They say a stray goat wandered in, caused a bit of chaos.
[03:15:47 TRACE The Living Valley] Encounter transcript T4 Kent->Marnie: Well, back to it—got to finish those reports before lunch.
[03:15:48 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:48 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:48 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:48 TRACE The Living Valley] Player2 stream line: {"message":"\u003cmartin\u003e It was wild, but the crowd's still buzzing about the missing props.","npc_id":"1b5799c5-7202-4d9b-9c70-ff32d8ab1918"}
[03:15:48 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:48 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:48 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:48 TRACE The Living Valley] Autonomy: Lewis found target Willy but out of talk range (dist=28.0).
[03:15:48 TRACE The Living Valley] Autonomy: Robin found target Alex but out of talk range (dist=34.0).
[03:15:48 TRACE The Living Valley] Autonomy: Abigail found target Jodi but out of talk range (dist=132.0).
[03:15:48 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=23.0).
[03:15:48 TRACE The Living Valley] Autonomy: Caroline found target Jodi but out of talk range (dist=26.0).
[03:15:48 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=43.0).
[03:15:48 TRACE The Living Valley] Autonomy: Jodi found target Pierre but out of talk range (dist=12.0).
[03:15:48 TRACE The Living Valley] Autonomy: Leah found target Penny but out of talk range (dist=37.0).
[03:15:48 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:48 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:48 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=75.0).
[03:15:48 TRACE The Living Valley] Autonomy: Penny found target Willy but out of talk range (dist=26.0).
[03:15:48 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=109.0).
[03:15:48 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:48 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=75.0).
[03:15:48 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=109.0).
[03:15:48 TRACE The Living Valley] Autonomy: Willy found target Lewis but out of talk range (dist=28.0).
[03:15:48 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=257.0).
[03:15:48 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:15:48 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:48 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:15:48 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=94.0).
[03:15:48 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:48 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:48 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=101.0).
[03:15:48 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:48 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:15:49 TRACE The Living Valley] Player2 stream line: {"message":"\u003cEvelyn\u003e Just the usual chaos when they try to be heroes, huh?","npc_id":"b36b9818-34dd-4a0a-ab78-7da6e433848b"}
[03:15:49 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:49 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:49 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:49 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:49 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:49 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:50 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:15:50 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:50 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:50 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:50 TRACE The Living Valley] Player2 stream line: {"message":"\u003cEmily\u003e I heard they think the props vanished during the last act’s blackout.","npc_id":"93aedf85-550d-4129-921a-0c89057e1104"}
[03:15:50 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:50 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:50 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:50 TRACE The Living Valley] Player2 stream line: {"message":"\u003cGeorge\u003e At least the kids got a story out of it, even if the street's a wreck.","npc_id":"e83bc960-3f11-49d0-8b92-f142f63f8305"}
[03:15:51 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:51 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:51 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:51 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:15:51 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:51 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:51 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:52 TRACE The Living Valley] Player2 stream line: {"message":"\u003cmartin\u003e Anyway, I’ll check the storage room—maybe they’re just misplaced.","npc_id":"1b5799c5-7202-4d9b-9c70-ff32d8ab1918"}
[03:15:52 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:52 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:52 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:52 DEBUG The Living Valley] Encounter conversation completed: Emily->Martin enc=enc_14 turns=4/4 duration_ms=9059.
[03:15:52 TRACE The Living Valley] Encounter transcript T1 Emily->Martin: Did you catch Andy and Susan's live show by the square? It was electrifying!
[03:15:52 TRACE The Living Valley] Encounter transcript T2 Martin->Emily: It was wild, but the crowd's still buzzing about the missing props.
[03:15:52 TRACE The Living Valley] Encounter transcript T3 Emily->Martin: I heard they think the props vanished during the last act’s blackout.
[03:15:52 TRACE The Living Valley] Encounter transcript T4 Martin->Emily: Anyway, I’ll check the storage room—maybe they’re just misplaced.
[03:15:52 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:52 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:52 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:15:52 TRACE The Living Valley] Player2 stream line: {"message":"\u003cEvelyn\u003e Well, back to our chores then—there's always more to mend.","npc_id":"b36b9818-34dd-4a0a-ab78-7da6e433848b"}
[03:15:53 DEBUG The Living Valley] Encounter conversation completed: George->Evelyn enc=enc_15 turns=4/4 duration_ms=9322.
[03:15:53 TRACE The Living Valley] Encounter transcript T1 George->Evelyn: That Andy and Susan's stunt left a mess on Main, eh?
[03:15:53 TRACE The Living Valley] Encounter transcript T2 Evelyn->George: Just the usual chaos when they try to be heroes, huh?
[03:15:53 TRACE The Living Valley] Encounter transcript T3 George->Evelyn: At least the kids got a story out of it, even if the street's a wreck.
[03:15:53 TRACE The Living Valley] Encounter transcript T4 Evelyn->George: Well, back to our chores then—there's always more to mend.
[03:15:53 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:15:53 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:53 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:53 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:53 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:53 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:53 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:53 TRACE The Living Valley] Autonomy: Lewis found target Willy but out of talk range (dist=29.0).
[03:15:53 TRACE The Living Valley] Autonomy: Robin found target Alex but out of talk range (dist=25.0).
[03:15:53 TRACE The Living Valley] Autonomy: Abigail found target Jodi but out of talk range (dist=132.0).
[03:15:53 TRACE The Living Valley] Autonomy: Alex found target Robin but out of talk range (dist=25.0).
[03:15:53 TRACE The Living Valley] Autonomy: Caroline found target Jodi but out of talk range (dist=17.0).
[03:15:53 TRACE The Living Valley] Autonomy: Gus found target Robin but out of talk range (dist=52.0).
[03:15:53 TRACE The Living Valley] Autonomy: Jodi found target Pierre but out of talk range (dist=7.0).
[03:15:53 TRACE The Living Valley] Autonomy: Leah found target Penny but out of talk range (dist=34.0).
[03:15:53 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:53 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:53 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=84.0).
[03:15:53 TRACE The Living Valley] Autonomy: Penny found target Willy but out of talk range (dist=25.0).
[03:15:53 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=119.0).
[03:15:53 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:53 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=84.0).
[03:15:53 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=119.0).
[03:15:53 TRACE The Living Valley] Autonomy: Willy found target Penny but out of talk range (dist=25.0).
[03:15:53 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=267.0).
[03:15:53 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:15:53 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:53 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:15:53 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=94.0).
[03:15:53 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:53 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:53 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=111.0).
[03:15:53 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:54 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:54 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:54 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:54 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:54 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:54 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:55 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:55 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:55 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:55 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:55 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:55 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:55 DEBUG The Living Valley] Autonomy: Gunther->GuntherSilvian encounter approved! block=ReturnHome location=ArchaeologyHouse.
[03:15:55 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian blocked by wall (no line of sight).
[03:15:56 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:56 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:56 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:56 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:56 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:56 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:57 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:57 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:57 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:57 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:57 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:57 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:58 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:58 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:58 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:58 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:58 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:58 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:58 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=23.0).
[03:15:58 TRACE The Living Valley] Autonomy: Robin found target Pierre but out of talk range (dist=12.0).
[03:15:58 TRACE The Living Valley] Autonomy: Abigail found target Jodi but out of talk range (dist=133.0).
[03:15:58 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=23.0).
[03:15:58 TRACE The Living Valley] Autonomy: Caroline found target Jodi but out of talk range (dist=12.0).
[03:15:58 TRACE The Living Valley] Autonomy: Gus found target Alex but out of talk range (dist=77.0).
[03:15:58 TRACE The Living Valley] Autonomy: Haley found target Leah but out of talk range (dist=21.0).
[03:15:58 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=78.0).
[03:15:58 TRACE The Living Valley] Autonomy: Jodi found target Caroline but out of talk range (dist=12.0).
[03:15:58 TRACE The Living Valley] Autonomy: Leah found target Haley but out of talk range (dist=21.0).
[03:15:58 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:15:58 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:15:58 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=93.0).
[03:15:58 TRACE The Living Valley] Autonomy: Penny found target Haley but out of talk range (dist=20.0).
[03:15:58 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=128.0).
[03:15:58 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:15:58 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=93.0).
[03:15:58 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=128.0).
[03:15:58 TRACE The Living Valley] Autonomy: Willy found target Penny but out of talk range (dist=24.0).
[03:15:58 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=276.0).
[03:15:58 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:15:58 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:15:58 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:15:58 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=94.0).
[03:15:58 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:15:58 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=78.0).
[03:15:58 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:15:58 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=120.0).
[03:15:58 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:15:59 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:15:59 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:59 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:59 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:15:59 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:15:59 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:00 TRACE The Living Valley] Autonomy: encounter enc_13 Marnie->Kent waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:00 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:00 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:00 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Marnie->Kent after complete.
[03:16:00 DEBUG The Living Valley] Autonomy: [HANDOFF] Marnie starting handoff: TilePoint=(18,90), controller=null, followSchedule=True, time=1120, map=Town.
[03:16:00 TRACE The Living Valley] Autonomy: queued Marnie for vanilla schedule resume after encounter enc_13 (complete, restored=False, next_tick=19621, map=Town, time=1120).
[03:16:00 DEBUG The Living Valley] Autonomy: [HANDOFF] Kent starting handoff: TilePoint=(20,90), controller=null, followSchedule=True, time=1120, map=Town.
[03:16:00 TRACE The Living Valley] Autonomy: queued Kent for vanilla schedule resume after encounter enc_13 (complete, restored=False, next_tick=19621, map=Town, time=1120).
[03:16:00 DEBUG The Living Valley] Autonomy: Player2 encounter enc_13 Marnie->Kent completed (outcome=friendly).
[03:16:00 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:00 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Marnie starting rebind at TilePoint=(18,90), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1120.
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Marnie cleared schedule, calling TryLoadSchedule().
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Marnie TryLoadSchedule returned=True, schedule_count=6, first_keys=900,1000,1300,1600,1810.
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Marnie current_time=1120, entries_before_current=900:AnimalShop,1000:SeedShop.
[03:16:00 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Marnie encounter=enc_13 from=Town to=SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30) arrival_resolved=True active_target_location=SeedShop active_target_tile=(23,16) time=1120.
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Marnie reset complete: lastAttemptedSchedule=1120, previousEndPoint=(43,56), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1000, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(23,16), active_facing=2, active_behavior=none, fallback_used=True.
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Kent starting rebind at TilePoint=(20,90), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1120.
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Kent cleared schedule, calling TryLoadSchedule().
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Kent TryLoadSchedule returned=True, schedule_count=7, first_keys=700,1030,1400,1700,1900.
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Kent current_time=1120, entries_before_current=700:Town,1030:SamHouse.
[03:16:00 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Kent encounter=enc_13 from=Town to=SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24) arrival_resolved=True active_target_location=SamHouse active_target_tile=(8,12) time=1120.
[03:16:00 DEBUG The Living Valley] Autonomy: [REBIND] Kent reset complete: lastAttemptedSchedule=1120, previousEndPoint=(10,85), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1030, next_schedule_time=1400, active_target_location=SamHouse, active_target_tile=(8,12), active_facing=2, active_behavior=none, fallback_used=True.
[03:16:01 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(19,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:01 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(19,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:01 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:01 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:01 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(18,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:01 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:01 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:02 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:02 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:02 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(17,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:02 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(20,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:02 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:02 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:02 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(16,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:02 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(21,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:03 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:03 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:03 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(15,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:03 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(22,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:03 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:03 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:03 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=23.0).
[03:16:03 TRACE The Living Valley] Autonomy: Robin found target Pierre but out of talk range (dist=9.0).
[03:16:03 TRACE The Living Valley] Autonomy: Abigail found target Jodi but out of talk range (dist=133.0).
[03:16:03 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=23.0).
[03:16:03 TRACE The Living Valley] Autonomy: Caroline found target Jodi but out of talk range (dist=9.0).
[03:16:03 TRACE The Living Valley] Autonomy: Gus found target Alex but out of talk range (dist=77.0).
[03:16:03 TRACE The Living Valley] Autonomy: Haley found target Marnie but out of talk range (dist=12.0).
[03:16:03 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=70.0).
[03:16:03 TRACE The Living Valley] Autonomy: Jodi found target Caroline but out of talk range (dist=9.0).
[03:16:03 TRACE The Living Valley] Autonomy: Leah found target Kent but out of talk range (dist=18.0).
[03:16:03 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:03 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:16:03 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=103.0).
[03:16:03 TRACE The Living Valley] Autonomy: Penny found target Marnie but out of talk range (dist=23.0).
[03:16:03 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=137.0).
[03:16:03 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:03 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=103.0).
[03:16:03 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=137.0).
[03:16:03 TRACE The Living Valley] Autonomy: Willy found target Haley but out of talk range (dist=18.0).
[03:16:03 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=286.0).
[03:16:03 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:03 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:03 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:03 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=94.0).
[03:16:03 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:03 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=70.0).
[03:16:03 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:16:03 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=129.0).
[03:16:03 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:03 DEBUG The Living Valley] Autonomy: [ARRIVAL] Daulton active-slot handoff at tile (44,35) in Beach (active_schedule_time=900, active_facing=1, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(43,35), facing=1, time=1120).
[03:16:03 DEBUG The Living Valley] Autonomy: returned Daulton to active-slot schedule action after encounter enc_9 (complete, restored=False, attempts=1, active_schedule_time=900, next_schedule_time=1200, active_target_location=Beach, active_target_tile=(44,35), active_facing=1, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(43,35), previousEndPoint=(44,35), lastAttemptedSchedule=1120, map=Beach, time=1120).
[03:16:03 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(14,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:03 DEBUG The Living Valley] Autonomy: [MONITOR] Daulton encounter=enc_9 tick=1: controller=PathFindController, isMoving=True, TilePoint=(43,35), moved_from_initial=yes, previousEndPoint=(44,35), followSchedule=True.
[03:16:03 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(23,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:04 DEBUG The Living Valley] Autonomy: [MONITOR] Daulton encounter=enc_9 tick=2: controller=PathFindController, isMoving=True, TilePoint=(43,35), moved_from_initial=yes, previousEndPoint=(44,35), followSchedule=True.
[03:16:04 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:04 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:04 DEBUG The Living Valley] Autonomy: [MONITOR] Daulton encounter=enc_9 tick=3: controller=PathFindController, isMoving=True, TilePoint=(43,35), moved_from_initial=yes, previousEndPoint=(44,35), followSchedule=True.
[03:16:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(13,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:04 DEBUG The Living Valley] Autonomy: [MONITOR] Daulton encounter=enc_9 tick=4: controller=PathFindController, isMoving=True, TilePoint=(44,35), moved_from_initial=yes, previousEndPoint=(44,35), followSchedule=True.
[03:16:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(24,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:04 DEBUG The Living Valley] Autonomy: [MONITOR] Daulton encounter=enc_9 tick=5: controller=PathFindController, isMoving=True, TilePoint=(44,35), moved_from_initial=yes, previousEndPoint=(44,35), followSchedule=True.
[03:16:04 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:04 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:04 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(12,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:05 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(25,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:05 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:05 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:05 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(11,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:05 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(26,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:05 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:05 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(10,90) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:06 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:06 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(27,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(10,89) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:06 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:06 TRACE The Living Valley] Autonomy: encounter enc_15 George->Evelyn waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:06 TRACE The Living Valley] Autonomy: Caroline->Jodi skipped by 50% encounter gate (block=BaseAnchor).
[03:16:06 TRACE The Living Valley] Autonomy: Jodi->Caroline skipped by 50% encounter gate (block=BaseAnchor).
[03:16:06 TRACE The Living Valley] Autonomy: Caroline->Jodi skipped by 50% encounter gate (block=BaseAnchor).
[03:16:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(28,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:07 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:07 TRACE The Living Valley] Autonomy: released vanilla encounter scene for George->Evelyn after complete.
[03:16:07 DEBUG The Living Valley] Autonomy: [HANDOFF] George starting handoff: TilePoint=(16,22), controller=null, followSchedule=True, time=1130, map=JoshHouse.
[03:16:07 TRACE The Living Valley] Autonomy: queued George for vanilla schedule resume after encounter enc_15 (complete, restored=False, next_tick=20011, map=JoshHouse, time=1130).
[03:16:07 DEBUG The Living Valley] Autonomy: [HANDOFF] Evelyn starting handoff: TilePoint=(13,22), controller=null, followSchedule=True, time=1130, map=JoshHouse.
[03:16:07 TRACE The Living Valley] Autonomy: queued Evelyn for vanilla schedule resume after encounter enc_15 (complete, restored=False, next_tick=20011, map=JoshHouse, time=1130).
[03:16:07 DEBUG The Living Valley] Autonomy: Player2 encounter enc_15 George->Evelyn completed (outcome=friendly).
[03:16:07 DEBUG The Living Valley] Autonomy: Caroline->Jodi encounter approved! block=BaseAnchor location=SeedShop.
[03:16:07 DEBUG The Living Valley] Autonomy: Caroline->Jodi staged successfully, starting conversation.
[03:16:07 DEBUG The Living Valley] Autonomy: Caroline->Jodi Player2 encounter conversation launched (turns=4, continuation=False).
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] George starting rebind at TilePoint=(16,22), controller=null, followSchedule=True, temporaryController=null, map=JoshHouse, time=1130.
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] George cleared schedule, calling TryLoadSchedule().
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] George TryLoadSchedule returned=True, schedule_count=4, first_keys=630,1200,1500,2000.
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] George current_time=1130, entries_before_current=630:JoshHouse.
[03:16:07 DEBUG The Living Valley] Autonomy: [FORCE_PATH] George already at active-slot destination after encounter enc_15 (active_schedule_time=630, next_schedule_time=1200, location=JoshHouse, tile=(16,22), time=1130).
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] George reset complete: lastAttemptedSchedule=1130, previousEndPoint=(16,22), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=630, next_schedule_time=1200, active_target_location=JoshHouse, active_target_tile=(16,22), active_facing=0, active_behavior=none, fallback_used=False.
[03:16:07 DEBUG The Living Valley] Autonomy: waiting to return George to vanilla schedule after encounter enc_15 (complete, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1130, active_schedule_time=630, next_schedule_time=1200, active_target_location=JoshHouse, active_target_tile=(16,22), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(16,22), previousEndPoint=(16,22), lastAttemptedSchedule=1130, map=JoshHouse, time=1130).
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] Evelyn starting rebind at TilePoint=(13,22), controller=null, followSchedule=True, temporaryController=null, map=JoshHouse, time=1130.
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] Evelyn cleared schedule, calling TryLoadSchedule().
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] Evelyn TryLoadSchedule returned=True, schedule_count=7, first_keys=800,1040,1210,1300,1630.
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] Evelyn current_time=1130, entries_before_current=800:JoshHouse,1040:JoshHouse.
[03:16:07 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Evelyn forced same-map active-slot path after encounter enc_15 (active_schedule_time=1040, next_schedule_time=1210, location=JoshHouse, tile=(17,22), time=1130).
[03:16:07 DEBUG The Living Valley] Autonomy: [REBIND] Evelyn reset complete: lastAttemptedSchedule=1130, previousEndPoint=(17,22), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1040, next_schedule_time=1210, active_target_location=JoshHouse, active_target_tile=(17,22), active_facing=3, active_behavior=none, fallback_used=True.
[03:16:07 DEBUG The Living Valley] Autonomy: [ARRIVAL] George active-slot handoff at tile (16,22) in JoshHouse (active_schedule_time=630, active_facing=0, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(16,22), facing=0, time=1130).
[03:16:07 DEBUG The Living Valley] Autonomy: returned George to active-slot schedule action after encounter enc_15 (complete, restored=False, attempts=1, active_schedule_time=630, next_schedule_time=1200, active_target_location=JoshHouse, active_target_tile=(16,22), active_facing=0, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(16,22), previousEndPoint=(16,22), lastAttemptedSchedule=1130, map=JoshHouse, time=1130).
[03:16:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(10,88) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(29,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:07 DEBUG The Living Valley] Autonomy: [MONITOR] George encounter=enc_15 tick=1: controller=null, isMoving=False, TilePoint=(16,22), moved_from_initial=no, previousEndPoint=(16,22), followSchedule=True.
[03:16:07 DEBUG The Living Valley] Autonomy: [MONITOR] George encounter=enc_15 tick=2: controller=null, isMoving=False, TilePoint=(16,22), moved_from_initial=no, previousEndPoint=(16,22), followSchedule=True.
[03:16:07 TRACE The Living Valley] Autonomy: encounter enc_14 Emily->Martin waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:07 DEBUG The Living Valley] Autonomy: [MONITOR] George encounter=enc_15 tick=3: controller=null, isMoving=False, TilePoint=(16,22), moved_from_initial=no, previousEndPoint=(16,22), followSchedule=True.
[03:16:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(10,87) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(30,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:07 DEBUG The Living Valley] Autonomy: [MONITOR] George encounter=enc_15 tick=4: controller=null, isMoving=False, TilePoint=(16,22), moved_from_initial=no, previousEndPoint=(16,22), followSchedule=True.
[03:16:08 DEBUG The Living Valley] Autonomy: [MONITOR] George encounter=enc_15 tick=5: controller=null, isMoving=False, TilePoint=(16,22), moved_from_initial=no, previousEndPoint=(16,22), followSchedule=True.
[03:16:08 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Emily->Martin after complete.
[03:16:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Emily starting handoff: TilePoint=(31,58), controller=null, followSchedule=True, time=1130, map=Town.
[03:16:08 TRACE The Living Valley] Autonomy: queued Emily for vanilla schedule resume after encounter enc_14 (complete, restored=False, next_tick=20071, map=Town, time=1130).
[03:16:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Martin starting handoff: TilePoint=(32,57), controller=null, followSchedule=True, time=1130, map=Town.
[03:16:08 TRACE The Living Valley] Autonomy: queued Martin for vanilla schedule resume after encounter enc_14 (complete, restored=False, next_tick=20071, map=Town, time=1130).
[03:16:08 DEBUG The Living Valley] Autonomy: Player2 encounter enc_14 Emily->Martin completed (outcome=friendly).
[03:16:08 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily starting rebind at TilePoint=(31,58), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1130.
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily cleared schedule, calling TryLoadSchedule().
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily TryLoadSchedule returned=True, schedule_count=5, first_keys=900,1000,1300,1600,2430.
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily current_time=1130, entries_before_current=900:HaleyHouse,1000:SeedShop.
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Emily encounter=enc_14 from=Town to=SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30) arrival_resolved=True active_target_location=SeedShop active_target_tile=(27,16) time=1130.
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily reset complete: lastAttemptedSchedule=1130, previousEndPoint=(43,56), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1000, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(27,16), active_facing=2, active_behavior=none, fallback_used=True.
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Martin starting rebind at TilePoint=(32,57), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1130.
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Martin cleared schedule, calling TryLoadSchedule().
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Martin TryLoadSchedule returned=True, schedule_count=4, first_keys=800,2200,2400,2410.
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Martin current_time=1130, entries_before_current=800:JojaMart.
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Martin encounter=enc_14 from=Custom_Martin_WarpRoom to=BusStop transition_tile=(0,3) approach_tile=(0,3) arrival_tile=(22,9) arrival_resolved=True active_target_location=JojaMart active_target_tile=(9,25) time=1130.
[03:16:08 DEBUG The Living Valley] Autonomy: [REBIND] Martin reset complete: lastAttemptedSchedule=1130, previousEndPoint=(0,3), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=800, next_schedule_time=2200, active_target_location=JojaMart, active_target_tile=(9,25), active_facing=1, active_behavior=Martin_Idle, fallback_used=True.
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(transition_ready)] Martin encounter=enc_14 map=Custom_Martin_WarpRoom tile=(1,3) transition_tile=(0,3) approach_tile=(0,3).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warping)] Martin encounter=enc_14 from=Custom_Martin_WarpRoom to=BusStop transition_tile=(0,3) approach_tile=(0,3) arrival_tile=(22,9).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(22,9) target_leg=Custom_Martin_WarpRoom->BusStop transition_tile=(0,3) approach_tile=(0,3) arrival_tile=(22,9).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warped)] Martin encounter=enc_14 reached BusStop from Custom_Martin_WarpRoom.
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Martin encounter=enc_14 from=BusStop to=Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54) arrival_resolved=True active_target_location=JojaMart active_target_tile=(9,25) time=1130.
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=Town tile=(10,86) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(transition_ready)] Kent encounter=enc_13 map=Town tile=(10,86) transition_tile=(10,85) approach_tile=(10,85).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warping)] Kent encounter=enc_13 from=Town to=SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Kent encounter=enc_13 map=SamHouse tile=(4,24) target_leg=Town->SamHouse transition_tile=(10,85) approach_tile=(10,85) arrival_tile=(4,24).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warped)] Kent encounter=enc_13 reached SamHouse from Town.
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(target_map)] Kent encounter=enc_13 reached target map SamHouse; switching to active-slot target fallback.
[03:16:08 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Kent forced same-map active-slot path after encounter enc_13 (active_schedule_time=1030, next_schedule_time=1400, location=SamHouse, tile=(8,12), time=1130).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(31,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(22,10) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(32,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:08 TRACE The Living Valley] Player2 stream line: {"message":"\u003cCaroline\u003e Did you catch Andy’s stunt with Susan? Seems the whole town’s still buzzing.","npc_id":"82daed6c-5128-4cff-bbe7-3868d3c9299e"}
[03:16:08 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:08 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=23.0).
[03:16:08 TRACE The Living Valley] Autonomy: Robin found target Pierre but out of talk range (dist=10.0).
[03:16:08 TRACE The Living Valley] Autonomy: Abigail found target Robin but out of talk range (dist=154.0).
[03:16:08 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=23.0).
[03:16:08 TRACE The Living Valley] Autonomy: Gus found target Emily but out of talk range (dist=46.0).
[03:16:08 TRACE The Living Valley] Autonomy: Haley found target Willy but out of talk range (dist=18.0).
[03:16:08 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=56.0).
[03:16:08 TRACE The Living Valley] Autonomy: Leah found target Haley but out of talk range (dist=25.0).
[03:16:08 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:08 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:16:08 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=112.0).
[03:16:08 TRACE The Living Valley] Autonomy: Penny found target Marnie but out of talk range (dist=16.0).
[03:16:08 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=21.0).
[03:16:08 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:08 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=112.0).
[03:16:08 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=147.0).
[03:16:08 TRACE The Living Valley] Autonomy: Willy found target Marnie but out of talk range (dist=8.0).
[03:16:08 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=9.0).
[03:16:08 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=295.0).
[03:16:08 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:08 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:08 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:08 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=94.0).
[03:16:08 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:08 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=56.0).
[03:16:08 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:16:08 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=139.0).
[03:16:08 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:08 DEBUG The Living Valley] Autonomy: [ARRIVAL] Evelyn active-slot handoff at tile (17,22) in JoshHouse (active_schedule_time=1040, active_facing=3, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(16,22), facing=1, time=1130).
[03:16:08 DEBUG The Living Valley] Autonomy: returned Evelyn to active-slot schedule action after encounter enc_15 (complete, restored=False, attempts=1, active_schedule_time=1040, next_schedule_time=1210, active_target_location=JoshHouse, active_target_tile=(17,22), active_facing=3, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(16,22), previousEndPoint=(17,22), lastAttemptedSchedule=1130, map=JoshHouse, time=1130).
[03:16:08 DEBUG The Living Valley] Autonomy: [MONITOR] Evelyn encounter=enc_15 tick=1: controller=PathFindController, isMoving=True, TilePoint=(16,22), moved_from_initial=yes, previousEndPoint=(17,22), followSchedule=True.
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(32,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(23,10) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:09 DEBUG The Living Valley] Autonomy: [MONITOR] Evelyn encounter=enc_15 tick=2: controller=PathFindController, isMoving=True, TilePoint=(16,22), moved_from_initial=yes, previousEndPoint=(17,22), followSchedule=True.
[03:16:09 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(33,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:09 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:09 DEBUG The Living Valley] Autonomy: [MONITOR] Evelyn encounter=enc_15 tick=3: controller=PathFindController, isMoving=True, TilePoint=(16,22), moved_from_initial=yes, previousEndPoint=(17,22), followSchedule=True.
[03:16:09 DEBUG The Living Valley] Autonomy: [MONITOR] Evelyn encounter=enc_15 tick=4: controller=PathFindController, isMoving=True, TilePoint=(17,22), moved_from_initial=yes, previousEndPoint=(17,22), followSchedule=True.
[03:16:09 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(33,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:09 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(24,10) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:09 DEBUG The Living Valley] Autonomy: [MONITOR] Evelyn encounter=enc_15 tick=5: controller=PathFindController, isMoving=True, TilePoint=(17,22), moved_from_initial=yes, previousEndPoint=(17,22), followSchedule=True.
[03:16:09 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(34,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:09 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:09 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=2s)
[03:16:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(34,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,10) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:10 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(35,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(35,90) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,11) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:10 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(36,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(35,89) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:11 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,12) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(37,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(35,88) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:11 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,13) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(38,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:12 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(35,87) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,14) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:12 TRACE The Living Valley] Player2 stream line: {"message":"\u003cJodi\u003e People still talking about it at the market—some say it’ll boost sales, others worry it’s a distraction.","npc_id":"2cf2cdd3-dd20-4835-855f-60f15a291671"}
[03:16:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(39,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:12 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(35,86) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,15) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(40,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:13 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(36,86) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,16) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(41,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:13 TRACE The Living Valley] Player2 stream line: {"message":"\u003cCaroline\u003e I hear the mayor’s already considering a yearly showcase—might be worth watching.","npc_id":"82daed6c-5128-4cff-bbe7-3868d3c9299e"}
[03:16:13 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:13 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=24.0).
[03:16:13 TRACE The Living Valley] Autonomy: Robin found target Pierre but out of talk range (dist=20.0).
[03:16:13 TRACE The Living Valley] Autonomy: Abigail found target Robin but out of talk range (dist=153.0).
[03:16:13 TRACE The Living Valley] Autonomy: Alex found target Emily but out of talk range (dist=29.0).
[03:16:13 TRACE The Living Valley] Autonomy: Gus found target Emily but out of talk range (dist=32.0).
[03:16:13 TRACE The Living Valley] Autonomy: Haley found target Willy but out of talk range (dist=20.0).
[03:16:13 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=66.0).
[03:16:13 TRACE The Living Valley] Autonomy: Leah found target Haley but out of talk range (dist=25.0).
[03:16:13 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:13 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:16:13 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=121.0).
[03:16:13 TRACE The Living Valley] Autonomy: Penny found target Marnie but out of talk range (dist=25.0).
[03:16:13 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=12.0).
[03:16:13 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:13 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=121.0).
[03:16:13 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=156.0).
[03:16:13 TRACE The Living Valley] Autonomy: Willy found target Marnie but out of talk range (dist=8.0).
[03:16:13 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=16.0).
[03:16:13 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=304.0).
[03:16:13 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:13 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:13 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:13 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=94.0).
[03:16:13 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:13 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=66.0).
[03:16:13 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:16:13 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=148.0).
[03:16:13 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(36,85) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,17) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(42,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:14 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:14 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(36,84) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,18) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(43,58) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:14 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(36,83) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,19) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:15 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=Town tile=(43,57) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(transition_ready)] Emily encounter=enc_14 map=Town tile=(43,57) transition_tile=(43,56) approach_tile=(43,56).
[03:16:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warping)] Emily encounter=enc_14 from=Town to=SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Emily encounter=enc_14 map=SeedShop tile=(6,30) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warped)] Emily encounter=enc_14 reached SeedShop from Town.
[03:16:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(target_map)] Emily encounter=enc_14 reached target map SeedShop; switching to active-slot target fallback.
[03:16:15 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Emily forced same-map active-slot path after encounter enc_14 (active_schedule_time=1000, next_schedule_time=1300, location=SeedShop, tile=(27,16), time=1140).
[03:16:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(36,82) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:15 TRACE The Living Valley] Player2 stream line: {"message":"\u003cJodi\u003e Anyway, got to finish my chores before the afternoon rush.","npc_id":"2cf2cdd3-dd20-4835-855f-60f15a291671"}
[03:16:15 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,20) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:15 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:15 DEBUG The Living Valley] Encounter conversation completed: Caroline->Jodi enc=enc_16 turns=4/4 duration_ms=8687.
[03:16:15 TRACE The Living Valley] Encounter transcript T1 Caroline->Jodi: Did you catch Andy’s stunt with Susan? Seems the whole town’s still buzzing.
[03:16:15 TRACE The Living Valley] Encounter transcript T2 Jodi->Caroline: People still talking about it at the market—some say it’ll boost sales, others worry it’s a distraction.
[03:16:15 TRACE The Living Valley] Encounter transcript T3 Caroline->Jodi: I hear the mayor’s already considering a yearly showcase—might be worth watching.
[03:16:15 TRACE The Living Valley] Encounter transcript T4 Jodi->Caroline: Anyway, got to finish my chores before the afternoon rush.
[03:16:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(36,81) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,21) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:16 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:16 DEBUG The Living Valley] Autonomy: [ARRIVAL] Kent active-slot handoff at tile (8,12) in SamHouse (active_schedule_time=1030, active_facing=2, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(8,13), facing=1, time=1140).
[03:16:16 DEBUG The Living Valley] Autonomy: returned Kent to active-slot schedule action after encounter enc_13 (complete, restored=False, attempts=1, active_schedule_time=1030, next_schedule_time=1400, active_target_location=SamHouse, active_target_tile=(8,12), active_facing=2, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(8,13), previousEndPoint=(8,12), lastAttemptedSchedule=1140, map=SamHouse, time=1140).
[03:16:16 DEBUG The Living Valley] Autonomy: [MONITOR] Kent encounter=enc_13 tick=1: controller=PathFindController, isMoving=True, TilePoint=(8,13), moved_from_initial=yes, previousEndPoint=(8,12), followSchedule=True.
[03:16:16 DEBUG The Living Valley] Autonomy: [MONITOR] Kent encounter=enc_13 tick=2: controller=PathFindController, isMoving=True, TilePoint=(8,13), moved_from_initial=yes, previousEndPoint=(8,12), followSchedule=True.
[03:16:16 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(36,80) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:16 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:16 DEBUG The Living Valley] Autonomy: [MONITOR] Kent encounter=enc_13 tick=3: controller=PathFindController, isMoving=True, TilePoint=(8,13), moved_from_initial=yes, previousEndPoint=(8,12), followSchedule=True.
[03:16:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(25,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:16 DEBUG The Living Valley] Autonomy: [MONITOR] Kent encounter=enc_13 tick=4: controller=PathFindController, isMoving=True, TilePoint=(9,13), moved_from_initial=yes, previousEndPoint=(8,12), followSchedule=True.
[03:16:17 DEBUG The Living Valley] Autonomy: [MONITOR] Kent encounter=enc_13 tick=5: controller=PathFindController, isMoving=True, TilePoint=(9,13), moved_from_initial=yes, previousEndPoint=(8,12), followSchedule=True.
[03:16:17 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(36,79) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:17 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:17 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(26,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:17 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,79) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:17 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:17 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(27,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:18 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:18 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,78) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:18 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(28,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:18 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:18 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=27.0).
[03:16:18 TRACE The Living Valley] Autonomy: Robin found target Emily but out of talk range (dist=22.0).
[03:16:18 TRACE The Living Valley] Autonomy: Abigail found target Robin but out of talk range (dist=165.0).
[03:16:18 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=32.0).
[03:16:18 TRACE The Living Valley] Autonomy: Gus found target Haley but out of talk range (dist=38.0).
[03:16:18 TRACE The Living Valley] Autonomy: Haley found target Willy but out of talk range (dist=22.0).
[03:16:18 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=85.0).
[03:16:18 TRACE The Living Valley] Autonomy: Leah found target Haley but out of talk range (dist=26.0).
[03:16:18 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:18 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:16:18 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=131.0).
[03:16:18 TRACE The Living Valley] Autonomy: Penny found target Marnie but out of talk range (dist=34.0).
[03:16:18 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=7.0).
[03:16:18 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:18 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=131.0).
[03:16:18 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=165.0).
[03:16:18 TRACE The Living Valley] Autonomy: Willy found target Marnie but out of talk range (dist=8.0).
[03:16:18 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=25.0).
[03:16:18 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=314.0).
[03:16:18 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:18 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:18 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:18 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=94.0).
[03:16:18 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:18 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=85.0).
[03:16:18 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:16:18 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=157.0).
[03:16:18 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:18 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,77) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:18 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(29,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:19 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:19 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,76) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:19 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(30,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:19 TRACE Farm Type Manager (FTM)] Spawned 1 objects. Time: 1150.
[03:16:19 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:19 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,75) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:19 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(31,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:20 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:20 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,74) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:20 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(32,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:20 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:20 TRACE The Living Valley] Autonomy: Sam->Kent skipped by 50% encounter gate (block=BaseAnchor).
[03:16:21 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,73) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:21 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(33,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:21 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:21 TRACE The Living Valley] Autonomy: Sam->Kent skipped by 50% encounter gate (block=BaseAnchor).
[03:16:21 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,72) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:21 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(34,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:21 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:21 TRACE The Living Valley] Autonomy: Sam->Kent skipped by 50% encounter gate (block=BaseAnchor).
[03:16:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,71) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(35,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:22 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,70) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:22 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:22 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(36,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:23 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:23 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,69) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:23 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(37,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:23 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:23 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=25.0).
[03:16:23 TRACE The Living Valley] Autonomy: Robin found target Emily but out of talk range (dist=17.0).
[03:16:23 TRACE The Living Valley] Autonomy: Abigail found target Robin but out of talk range (dist=170.0).
[03:16:23 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=25.0).
[03:16:23 TRACE The Living Valley] Autonomy: Gus found target Willy but out of talk range (dist=37.0).
[03:16:23 TRACE The Living Valley] Autonomy: Haley found target Willy but out of talk range (dist=21.0).
[03:16:23 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=103.0).
[03:16:23 TRACE The Living Valley] Autonomy: Leah found target Martin but out of talk range (dist=6.0).
[03:16:23 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:23 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=15.0).
[03:16:23 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=140.0).
[03:16:23 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=50.0).
[03:16:23 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=7.0).
[03:16:23 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:23 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=140.0).
[03:16:23 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:16:23 TRACE The Living Valley] Autonomy: Willy found target Marnie but out of talk range (dist=9.0).
[03:16:23 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=34.0).
[03:16:23 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=323.0).
[03:16:23 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:23 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:23 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:23 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=94.0).
[03:16:23 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:23 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=103.0).
[03:16:23 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:16:23 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=167.0).
[03:16:23 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:23 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,68) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:23 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(38,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:24 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:24 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,67) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:24 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(39,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:24 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:24 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(37,66) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:24 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_14 map=BusStop tile=(40,22) target_leg=BusStop->Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54).
[03:16:25 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:25 DEBUG The Living Valley] Autonomy: Leah->Martin encounter approved! block=BaseAnchor location=BusStop.
[03:16:25 DEBUG The Living Valley] Autonomy: Leah->Martin staged successfully, starting conversation.
[03:16:25 DEBUG The Living Valley] Autonomy: Leah->Martin Player2 encounter conversation launched (turns=4, continuation=False).
[03:16:25 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(38,66) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:25 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:25 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(38,65) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:26 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:26 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:26 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(38,64) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:26 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:26 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:26 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(39,64) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:27 TRACE The Living Valley] Player2 stream line: {"message":"\u003cLeah\u003e Did you catch Andy’s juggling act? It really lifted the town’s mood.","npc_id":"6699e37f-7aa8-41fb-b92a-8660e7d697bc"}
[03:16:27 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:27 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:27 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(40,64) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:27 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:27 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:27 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:28 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(41,64) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:28 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:28 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:28 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(42,64) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:28 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:28 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:28 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=18.0).
[03:16:28 TRACE The Living Valley] Autonomy: Robin found target Emily but out of talk range (dist=18.0).
[03:16:28 TRACE The Living Valley] Autonomy: Abigail found target Robin but out of talk range (dist=179.0).
[03:16:28 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=22.0).
[03:16:28 TRACE The Living Valley] Autonomy: Emily found target Pierre but out of talk range (dist=14.0).
[03:16:28 TRACE The Living Valley] Autonomy: Gus found target Marnie but out of talk range (dist=25.0).
[03:16:28 TRACE The Living Valley] Autonomy: Haley found target Gus but out of talk range (dist=29.0).
[03:16:28 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=119.0).
[03:16:28 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=16.0).
[03:16:28 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=18.0).
[03:16:28 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:28 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=5.0).
[03:16:28 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=150.0).
[03:16:28 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=53.0).
[03:16:28 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=16.0).
[03:16:28 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:28 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=150.0).
[03:16:28 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:16:28 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=5.0).
[03:16:28 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=332.0).
[03:16:28 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:28 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:28 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:28 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=98.0).
[03:16:28 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:28 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=119.0).
[03:16:28 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:16:28 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=176.0).
[03:16:28 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:28 TRACE The Living Valley] Player2 stream line: {"message":"\u003cmartin\u003e Those throws were slick, but I saw the ribbons snag on a fence.","npc_id":"1b5799c5-7202-4d9b-9c70-ff32d8ab1918"}
[03:16:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(43,64) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:29 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:29 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:29 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:29 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:29 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(43,63) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:29 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:30 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:30 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:30 TRACE The Living Valley] Player2 stream line: {"message":"\u003cLeah\u003e Now the fence’s torn—someone’s gotta fix it before the kids run into it.","npc_id":"6699e37f-7aa8-41fb-b92a-8660e7d697bc"}
[03:16:30 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(43,62) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:30 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:30 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:30 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(43,61) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:31 TRACE The Living Valley] Autonomy: encounter enc_16 Caroline->Jodi waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:16:31 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:31 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=2s)
[03:16:31 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(43,60) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:31 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Caroline->Jodi after complete.
[03:16:31 DEBUG The Living Valley] Autonomy: [HANDOFF] Caroline starting handoff: TilePoint=(24,17), controller=null, followSchedule=True, time=1200, map=SeedShop.
[03:16:31 TRACE The Living Valley] Autonomy: queued Caroline for vanilla schedule resume after encounter enc_16 (complete, restored=False, next_tick=21481, map=SeedShop, time=1200).
[03:16:31 DEBUG The Living Valley] Autonomy: [HANDOFF] Jodi starting handoff: TilePoint=(22,17), controller=null, followSchedule=True, time=1200, map=SeedShop.
[03:16:31 TRACE The Living Valley] Autonomy: queued Jodi for vanilla schedule resume after encounter enc_16 (complete, restored=False, next_tick=21481, map=SeedShop, time=1200).
[03:16:31 DEBUG The Living Valley] Autonomy: Player2 encounter enc_16 Caroline->Jodi completed (outcome=friendly).
[03:16:31 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Caroline starting rebind at TilePoint=(24,17), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1200.
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Caroline cleared schedule, calling TryLoadSchedule().
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Caroline TryLoadSchedule returned=True, schedule_count=6, first_keys=800,1030,1300,1600,1810.
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Caroline current_time=1200, entries_before_current=800:SeedShop,1030:SeedShop.
[03:16:31 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Caroline already at active-slot destination after encounter enc_16 (active_schedule_time=1030, next_schedule_time=1300, location=SeedShop, tile=(24,17), time=1200).
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Caroline reset complete: lastAttemptedSchedule=1200, previousEndPoint=(24,17), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1030, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(24,17), active_facing=3, active_behavior=none, fallback_used=False.
[03:16:31 DEBUG The Living Valley] Autonomy: waiting to return Caroline to vanilla schedule after encounter enc_16 (complete, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1200, active_schedule_time=1030, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(24,17), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(24,17), previousEndPoint=(24,17), lastAttemptedSchedule=1200, map=SeedShop, time=1200).
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Jodi starting rebind at TilePoint=(22,17), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1200.
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Jodi cleared schedule, calling TryLoadSchedule().
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Jodi TryLoadSchedule returned=True, schedule_count=8, first_keys=800,940,1000,1300,1600.
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Jodi current_time=1200, entries_before_current=800:SamHouse,940:SamHouse,1000:SeedShop.
[03:16:31 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Jodi already at active-slot destination after encounter enc_16 (active_schedule_time=1000, next_schedule_time=1300, location=SeedShop, tile=(22,17), time=1200).
[03:16:31 DEBUG The Living Valley] Autonomy: [REBIND] Jodi reset complete: lastAttemptedSchedule=1200, previousEndPoint=(22,17), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1000, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(22,17), active_facing=1, active_behavior=none, fallback_used=False.
[03:16:31 DEBUG The Living Valley] Autonomy: waiting to return Jodi to vanilla schedule after encounter enc_16 (complete, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1200, active_schedule_time=1000, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(22,17), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(22,17), previousEndPoint=(22,17), lastAttemptedSchedule=1200, map=SeedShop, time=1200).
[03:16:31 DEBUG The Living Valley] Autonomy: [ARRIVAL] Caroline active-slot handoff at tile (24,17) in SeedShop (active_schedule_time=1030, active_facing=3, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(24,17), facing=3, time=1200).
[03:16:31 DEBUG The Living Valley] Autonomy: returned Caroline to active-slot schedule action after encounter enc_16 (complete, restored=False, attempts=1, active_schedule_time=1030, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(24,17), active_facing=3, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(24,17), previousEndPoint=(24,17), lastAttemptedSchedule=1200, map=SeedShop, time=1200).
[03:16:31 DEBUG The Living Valley] Autonomy: [ARRIVAL] Jodi active-slot handoff at tile (22,17) in SeedShop (active_schedule_time=1000, active_facing=1, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(22,17), facing=1, time=1200).
[03:16:31 DEBUG The Living Valley] Autonomy: returned Jodi to active-slot schedule action after encounter enc_16 (complete, restored=False, attempts=1, active_schedule_time=1000, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(22,17), active_facing=1, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(22,17), previousEndPoint=(22,17), lastAttemptedSchedule=1200, map=SeedShop, time=1200).
[03:16:31 TRACE The Living Valley] Player2 stream line: {"message":"\u003cmartin\u003e Well, back to Joja, but I’ll flag the fence for repairs soon.","npc_id":"1b5799c5-7202-4d9b-9c70-ff32d8ab1918"}
[03:16:31 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_16 tick=1: controller=PathFindController, isMoving=True, TilePoint=(21,17), moved_from_initial=yes, previousEndPoint=(22,17), followSchedule=True.
[03:16:31 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_16 tick=1: controller=PathFindController, isMoving=True, TilePoint=(24,17), moved_from_initial=no, previousEndPoint=(24,17), followSchedule=True.
[03:16:31 DEBUG The Living Valley] Encounter conversation completed: Leah->Martin enc=enc_17 turns=4/4 duration_ms=6738.
[03:16:31 TRACE The Living Valley] Encounter transcript T1 Leah->Martin: Did you catch Andy’s juggling act? It really lifted the town’s mood.
[03:16:31 TRACE The Living Valley] Encounter transcript T2 Martin->Leah: Those throws were slick, but I saw the ribbons snag on a fence.
[03:16:31 TRACE The Living Valley] Encounter transcript T3 Leah->Martin: Now the fence’s torn—someone’s gotta fix it before the kids run into it.
[03:16:31 TRACE The Living Valley] Encounter transcript T4 Martin->Leah: Well, back to Joja, but I’ll flag the fence for repairs soon.
[03:16:32 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Marnie encounter=enc_13 map=Town tile=(43,59) target_leg=Town->SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30).
[03:16:32 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_16 tick=2: controller=PathFindController, isMoving=True, TilePoint=(21,17), moved_from_initial=yes, previousEndPoint=(22,17), followSchedule=True.
[03:16:32 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_16 tick=2: controller=PathFindController, isMoving=True, TilePoint=(23,17), moved_from_initial=yes, previousEndPoint=(24,17), followSchedule=True.
[03:16:32 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:32 TRACE SpaceCore] Event: BeforeWarp
[03:16:32 TRACE game] Warping to SeedShop
[03:16:32 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_16 tick=3: controller=PathFindController, isMoving=True, TilePoint=(21,17), moved_from_initial=yes, previousEndPoint=(22,17), followSchedule=True.
[03:16:32 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_16 tick=3: controller=PathFindController, isMoving=True, TilePoint=(23,17), moved_from_initial=yes, previousEndPoint=(24,17), followSchedule=True.
[03:16:32 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_16 tick=4: controller=PathFindController, isMoving=True, TilePoint=(21,17), moved_from_initial=yes, previousEndPoint=(22,17), followSchedule=True.
[03:16:32 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_16 tick=4: controller=PathFindController, isMoving=True, TilePoint=(23,17), moved_from_initial=yes, previousEndPoint=(24,17), followSchedule=True.
[03:16:32 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_16 tick=5: controller=PathFindController, isMoving=True, TilePoint=(21,17), moved_from_initial=yes, previousEndPoint=(22,17), followSchedule=True.
[03:16:32 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_16 tick=5: controller=PathFindController, isMoving=True, TilePoint=(23,17), moved_from_initial=yes, previousEndPoint=(24,17), followSchedule=True.
[03:16:32 TRACE The Living Valley] Autonomy: encounter enc_17 Leah->Martin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:32 TRACE SMAPI] Content Patcher edited Data/Events/SeedShop (for the 'Stardew Valley Expanded' content pack).
[03:16:32 TRACE The Living Valley] Autonomy: cancelling encounter enc_17 Leah->Martin (ui_interrupt).
[03:16:32 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Leah->Martin after ui_interrupt.
[03:16:32 DEBUG The Living Valley] Autonomy: [HANDOFF] Leah starting handoff: TilePoint=(42,23), controller=null, followSchedule=True, time=1200, map=BusStop.
[03:16:32 TRACE The Living Valley] Autonomy: queued Leah for vanilla schedule resume after encounter enc_17 (ui_interrupt, restored=False, next_tick=21549, map=BusStop, time=1200).
[03:16:32 DEBUG The Living Valley] Autonomy: [HANDOFF] Martin starting handoff: TilePoint=(40,22), controller=null, followSchedule=True, time=1200, map=BusStop.
[03:16:32 TRACE The Living Valley] Autonomy: queued Martin for vanilla schedule resume after encounter enc_17 (ui_interrupt, restored=False, next_tick=21549, map=BusStop, time=1200).
[03:16:32 TRACE Content Patcher] Requested cache invalidation for all assets matching a predicate.
[03:16:32 TRACE SMAPI] Content Patcher loaded asset 'Maps/Mountain' (for the 'Stardew Valley Expanded' content pack).
[03:16:32 TRACE SMAPI] Content Patcher edited Maps/Mountain (for the 'Stardew Valley Expanded' content pack).
[03:16:32 TRACE SMAPI] Content Patcher edited Maps/Mountain (for the 'Downhill Project' content pack).
[03:16:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Forest' (for the 'Stardew Valley Expanded' content pack).
[03:16:33 TRACE SMAPI] Content Patcher edited Maps/Forest (for the 'Stardew Valley Expanded' content pack).
[03:16:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Custom_GrampletonSuburbs' (for the 'Stardew Valley Expanded' content pack).
[03:16:33 TRACE SMAPI] Content Patcher edited Maps/Custom_GrampletonSuburbs (for the 'Stardew Valley Expanded' content pack).
[03:16:33 TRACE SMAPI] Invalidated 4 asset names (Maps/Custom_GrampletonSuburbs, Maps/Forest, Maps/Mountain, Maps/winter_outdoorsTileSheet).
Propagated 4 core assets (Maps/Custom_GrampletonSuburbs, Maps/Forest, Maps/Mountain, Maps/winter_outdoorsTileSheet).
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Martin starting rebind at TilePoint=(40,22), controller=null, followSchedule=True, temporaryController=null, map=BusStop, time=1200.
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Martin cleared schedule, calling TryLoadSchedule().
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Martin TryLoadSchedule returned=True, schedule_count=4, first_keys=800,2200,2400,2410.
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Martin current_time=1200, entries_before_current=800:JojaMart.
[03:16:33 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Martin encounter=enc_17 from=Custom_Martin_WarpRoom to=BusStop transition_tile=(0,3) approach_tile=(0,3) arrival_tile=(22,9) arrival_resolved=True active_target_location=JojaMart active_target_tile=(9,25) time=1200.
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Martin reset complete: lastAttemptedSchedule=1200, previousEndPoint=(0,3), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=800, next_schedule_time=2200, active_target_location=JojaMart, active_target_tile=(9,25), active_facing=1, active_behavior=Martin_Idle, fallback_used=True.
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Leah starting rebind at TilePoint=(42,23), controller=null, followSchedule=True, temporaryController=null, map=BusStop, time=1200.
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Leah cleared schedule, calling TryLoadSchedule().
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Leah TryLoadSchedule returned=True, schedule_count=6, first_keys=900,1030,1300,1450,2000.
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Leah current_time=1200, entries_before_current=900:LeahHouse,1030:Downhill.
[03:16:33 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Leah encounter=enc_17 from=BusStop to=Downhill transition_tile=(21,2) approach_tile=(21,2) arrival_tile=(28,80) arrival_resolved=True active_target_location=Downhill active_target_tile=(53,43) time=1200.
[03:16:33 DEBUG The Living Valley] Autonomy: [REBIND] Leah reset complete: lastAttemptedSchedule=1200, previousEndPoint=(21,2), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1030, next_schedule_time=1300, active_target_location=Downhill, active_target_tile=(53,43), active_facing=2, active_behavior=none, fallback_used=True.
[03:16:33 DEBUG The Living Valley] Autonomy: [CrossMapLeg(transition_ready)] Martin encounter=enc_17 map=Custom_Martin_WarpRoom tile=(1,3) transition_tile=(0,3) approach_tile=(0,3).
[03:16:33 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warping)] Martin encounter=enc_17 from=Custom_Martin_WarpRoom to=BusStop transition_tile=(0,3) approach_tile=(0,3) arrival_tile=(22,9).
[03:16:33 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Martin encounter=enc_17 map=BusStop tile=(22,9) target_leg=Custom_Martin_WarpRoom->BusStop transition_tile=(0,3) approach_tile=(0,3) arrival_tile=(22,9).
[03:16:33 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warped)] Martin encounter=enc_17 reached BusStop from Custom_Martin_WarpRoom.
[03:16:33 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Martin encounter=enc_17 from=BusStop to=Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54) arrival_resolved=True active_target_location=JojaMart active_target_tile=(9,25) time=1200.
[03:16:33 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=16.0).
[03:16:33 TRACE The Living Valley] Autonomy: Robin found target Caroline but out of talk range (dist=5.0).
[03:16:33 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=5.0).
[03:16:33 TRACE The Living Valley] Autonomy: Alex found target Pam but out of talk range (dist=12.0).
[03:16:33 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=67.0).
[03:16:33 TRACE The Living Valley] Autonomy: Emily found target Jodi but out of talk range (dist=7.0).
[03:16:33 TRACE The Living Valley] Autonomy: Gus found target Marnie but out of talk range (dist=13.0).
[03:16:33 TRACE The Living Valley] Autonomy: Haley found target Marnie but out of talk range (dist=42.0).
[03:16:33 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=120.0).
[03:16:33 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=22.0).
[03:16:33 TRACE The Living Valley] Autonomy: Marnie found target Gus but out of talk range (dist=13.0).
[03:16:33 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:33 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:16:33 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=156.0).
[03:16:33 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=12.0).
[03:16:33 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=57.0).
[03:16:33 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=22.0).
[03:16:33 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:33 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=156.0).
[03:16:33 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=169.0).
[03:16:33 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:16:33 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:16:33 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=339.0).
[03:16:33 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:33 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:33 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:33 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:16:33 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:33 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=120.0).
[03:16:33 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=10.0).
[03:16:33 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=67.0).
[03:16:33 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=183.0).
[03:16:33 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:33 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(stale)] Marnie encounter=enc_13 leg=Town->SeedShop tile=(43,59) map=Town retry_count=0.
[03:16:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Marnie encounter=enc_13 from=Town to=SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30) arrival_resolved=True active_target_location=SeedShop active_target_tile=(23,16) time=1200.
[03:16:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(retry)] Marnie encounter=enc_13 restarted leg toward SeedShop from Town using transition_tile=(43,56) approach_tile=(43,56) retry_count=1.
[03:16:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(stale)] Leah encounter=enc_17 leg=BusStop->Downhill tile=(42,23) map=BusStop retry_count=0.
[03:16:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Leah encounter=enc_17 from=BusStop to=Downhill transition_tile=(21,2) approach_tile=(21,2) arrival_tile=(28,80) arrival_resolved=True active_target_location=Downhill active_target_tile=(53,43) time=1200.
[03:16:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(retry)] Leah encounter=enc_17 restarted leg toward Downhill from BusStop using transition_tile=(21,2) approach_tile=(21,2) retry_count=1.
[03:16:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(stale)] Martin encounter=enc_17 leg=BusStop->Town tile=(22,9) map=BusStop retry_count=0.
[03:16:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Martin encounter=enc_17 from=BusStop to=Town transition_tile=(44,22) approach_tile=(44,22) arrival_tile=(0,54) arrival_resolved=True active_target_location=JojaMart active_target_tile=(9,25) time=1200.
[03:16:35 DEBUG The Living Valley] Autonomy: [CrossMapLeg(retry)] Martin encounter=enc_17 restarted leg toward Town from BusStop using transition_tile=(44,22) approach_tile=(44,22) retry_count=1.
[03:16:36 TRACE SpaceCore] Event: OnEventFinished
[03:16:36 TRACE SpaceCore] Event: BeforeWarp
[03:16:36 TRACE game] Warping to SeedShop
[03:16:36 TRACE SMAPI] Content Patcher edited Strings/SpeechBubbles (for the 'Stardew Valley Expanded' content pack).
[03:16:38 DEBUG The Living Valley] Autonomy: [CrossMapLeg(stale)] Marnie encounter=enc_13 leg=Town->SeedShop tile=(43,59) map=Town retry_count=1.
[03:16:38 WARN  The Living Valley] Autonomy: [CrossMapLeg(stale)] Marnie encounter=enc_13 exceeded retry limit for leg Town->SeedShop; clearing fallback until next vanilla schedule boundary.
[03:16:38 DEBUG The Living Valley] Autonomy: returned Marnie to vanilla schedule after encounter enc_13 (complete, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1120, active_schedule_time=1000, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(23,16), fallback_used=False, resumed=true, method=VanillaSchedule(update), controller=null, isMoving=True, temporary_controller=False, TilePoint=(43,59), previousEndPoint=(43,56), lastAttemptedSchedule=1200, map=Town, time=1200).
[03:16:38 DEBUG The Living Valley] Autonomy: [MONITOR] Marnie encounter=enc_13 tick=1: controller=null, isMoving=True, TilePoint=(43,59), moved_from_initial=yes, previousEndPoint=(43,56), followSchedule=True.
[03:16:38 DEBUG The Living Valley] Autonomy: [MONITOR] Marnie encounter=enc_13 tick=2: controller=null, isMoving=True, TilePoint=(43,59), moved_from_initial=yes, previousEndPoint=(43,56), followSchedule=True.
[03:16:38 DEBUG The Living Valley] Autonomy: [MONITOR] Marnie encounter=enc_13 tick=3: controller=null, isMoving=True, TilePoint=(43,59), moved_from_initial=yes, previousEndPoint=(43,56), followSchedule=True.
[03:16:38 DEBUG The Living Valley] Autonomy: [MONITOR] Marnie encounter=enc_13 tick=4: controller=null, isMoving=True, TilePoint=(43,59), moved_from_initial=yes, previousEndPoint=(43,56), followSchedule=True.
[03:16:38 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=16.0).
[03:16:38 TRACE The Living Valley] Autonomy: Robin found target Caroline but out of talk range (dist=5.0).
[03:16:38 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=5.0).
[03:16:38 TRACE The Living Valley] Autonomy: Alex found target Pam but out of talk range (dist=12.0).
[03:16:38 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=67.0).
[03:16:38 TRACE The Living Valley] Autonomy: Emily found target Jodi but out of talk range (dist=7.0).
[03:16:38 TRACE The Living Valley] Autonomy: Gus found target Marnie but out of talk range (dist=13.0).
[03:16:38 TRACE The Living Valley] Autonomy: Haley found target Marnie but out of talk range (dist=42.0).
[03:16:38 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=120.0).
[03:16:38 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=22.0).
[03:16:38 TRACE The Living Valley] Autonomy: Marnie found target Gus but out of talk range (dist=13.0).
[03:16:38 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:38 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:16:38 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=156.0).
[03:16:38 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=12.0).
[03:16:38 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=57.0).
[03:16:38 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=22.0).
[03:16:38 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:38 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=156.0).
[03:16:38 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=169.0).
[03:16:38 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:16:38 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:16:38 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=339.0).
[03:16:38 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:38 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:38 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:38 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:16:38 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:38 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=120.0).
[03:16:38 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=10.0).
[03:16:38 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=67.0).
[03:16:38 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=183.0).
[03:16:38 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:38 DEBUG The Living Valley] Autonomy: [CrossMapLeg(stale)] Leah encounter=enc_17 leg=BusStop->Downhill tile=(42,23) map=BusStop retry_count=1.
[03:16:38 WARN  The Living Valley] Autonomy: [CrossMapLeg(stale)] Leah encounter=enc_17 exceeded retry limit for leg BusStop->Downhill; clearing fallback until next vanilla schedule boundary.
[03:16:38 DEBUG The Living Valley] Autonomy: [MONITOR] Marnie encounter=enc_13 tick=5: controller=null, isMoving=True, TilePoint=(43,59), moved_from_initial=yes, previousEndPoint=(43,56), followSchedule=True.
[03:16:38 DEBUG The Living Valley] Autonomy: [CrossMapLeg(stale)] Martin encounter=enc_17 leg=BusStop->Town tile=(22,9) map=BusStop retry_count=1.
[03:16:38 WARN  The Living Valley] Autonomy: [CrossMapLeg(stale)] Martin encounter=enc_17 exceeded retry limit for leg BusStop->Town; clearing fallback until next vanilla schedule boundary.
[03:16:38 TRACE SpaceCore] Event: OnEventFinished
[03:16:38 TRACE SpaceCore] Event: BeforeWarp
[03:16:38 TRACE game] Warping to SeedShop
[03:16:41 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian skipped by 50% encounter gate (block=ReturnHome).
[03:16:41 TRACE The Living Valley] Autonomy: GuntherSilvian->Gunther skipped by 50% encounter gate (block=BaseAnchor).
[03:16:41 TRACE SpaceCore] Event: OnEventFinished
[03:16:41 TRACE SpaceCore] Event: BeforeWarp
[03:16:41 TRACE game] Warping to SeedShop
[03:16:41 TRACE The Living Valley] Ambient command unlocks day 121: adjust_town_sentiment | events=5 public=2 market=0 scarcity=0 oversupply=0 anomaly=False
[03:16:41 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian skipped by 50% encounter gate (block=ReturnHome).
[03:16:41 DEBUG The Living Valley] Autonomy: GuntherSilvian->Gunther encounter approved! block=BaseAnchor location=ArchaeologyHouse.
[03:16:41 TRACE The Living Valley] Autonomy: GuntherSilvian->Gunther blocked by wall (no line of sight).
[03:16:43 TRACE SpaceCore] Event: OnEventFinished
[03:16:43 TRACE SpaceCore] Event: BeforeWarp
[03:16:43 TRACE game] Warping to SeedShop
[03:16:43 TRACE SMAPI] Content Patcher loaded asset 'Portraits/Morris' (for the 'Stardew Valley Expanded' content pack).
[03:16:43 TRACE SMAPI] Content Patcher loaded asset 'Characters/Morris' (for the 'Stardew Valley Expanded' content pack).
[03:16:43 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=16.0).
[03:16:43 TRACE The Living Valley] Autonomy: Robin found target Caroline but out of talk range (dist=5.0).
[03:16:43 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=5.0).
[03:16:43 TRACE The Living Valley] Autonomy: Alex found target Pam but out of talk range (dist=12.0).
[03:16:43 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=67.0).
[03:16:43 TRACE The Living Valley] Autonomy: Emily found target Jodi but out of talk range (dist=7.0).
[03:16:43 TRACE The Living Valley] Autonomy: Gus found target Marnie but out of talk range (dist=13.0).
[03:16:43 TRACE The Living Valley] Autonomy: Haley found target Marnie but out of talk range (dist=42.0).
[03:16:43 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=120.0).
[03:16:43 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=22.0).
[03:16:43 TRACE The Living Valley] Autonomy: Marnie found target Gus but out of talk range (dist=13.0).
[03:16:43 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:43 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:16:43 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=156.0).
[03:16:43 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=12.0).
[03:16:43 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=57.0).
[03:16:43 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=22.0).
[03:16:43 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:43 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=156.0).
[03:16:43 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=169.0).
[03:16:43 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:16:43 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:16:43 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=339.0).
[03:16:43 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:43 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:43 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:43 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:16:43 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:43 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=120.0).
[03:16:43 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=10.0).
[03:16:43 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=67.0).
[03:16:43 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=183.0).
[03:16:43 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:47 TRACE SpaceCore] Event: OnEventFinished
[03:16:47 TRACE SpaceCore] Event: BeforeWarp
[03:16:47 TRACE game] Warping to SeedShop
[03:16:48 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=15.0).
[03:16:48 TRACE The Living Valley] Autonomy: Robin found target Caroline but out of talk range (dist=5.0).
[03:16:48 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=5.0).
[03:16:48 TRACE The Living Valley] Autonomy: Alex found target Pam but out of talk range (dist=11.0).
[03:16:48 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=67.0).
[03:16:48 TRACE The Living Valley] Autonomy: Emily found target Jodi but out of talk range (dist=5.0).
[03:16:48 TRACE The Living Valley] Autonomy: Gus found target Marnie but out of talk range (dist=12.0).
[03:16:48 TRACE The Living Valley] Autonomy: Haley found target Marnie but out of talk range (dist=41.0).
[03:16:48 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=120.0).
[03:16:48 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=23.0).
[03:16:48 TRACE The Living Valley] Autonomy: Marnie found target Gus but out of talk range (dist=12.0).
[03:16:48 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:48 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:16:48 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=157.0).
[03:16:48 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=11.0).
[03:16:48 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=58.0).
[03:16:48 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=23.0).
[03:16:48 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:48 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=157.0).
[03:16:48 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:16:48 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:16:48 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:16:48 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=339.0).
[03:16:48 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:48 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:48 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:48 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:16:48 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:48 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=120.0).
[03:16:48 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=11.0).
[03:16:48 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=67.0).
[03:16:48 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=183.0).
[03:16:48 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:49 TRACE The Living Valley] Autonomy: Emily->Caroline skipped by 50% encounter gate (block=BaseAnchor).
[03:16:50 DEBUG The Living Valley] Autonomy: Emily->Caroline encounter approved! block=BaseAnchor location=SeedShop.
[03:16:50 DEBUG The Living Valley] Autonomy: Emily->Caroline staged successfully, starting conversation.
[03:16:50 DEBUG The Living Valley] Autonomy: Emily->Caroline Player2 encounter conversation launched (turns=4, continuation=False).
[03:16:51 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:51 TRACE The Living Valley] Autonomy: Abigail->Pierre skipped by 50% encounter gate (block=BaseAnchor).
[03:16:51 TRACE The Living Valley] Player2 stream line: {"message":"\u003cEmily\u003e Andy and Susan’s show left the town square humming, especially after that surprise song.","npc_id":"93aedf85-550d-4129-921a-0c89057e1104"}
[03:16:51 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:52 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:52 DEBUG The Living Valley] Autonomy: Lewis->Gus encounter approved! block=BaseAnchor location=Town.
[03:16:52 DEBUG The Living Valley] Autonomy: Lewis->Gus staged successfully, starting conversation.
[03:16:52 DEBUG The Living Valley] Autonomy: Lewis->Gus Player2 encounter conversation launched (turns=4, continuation=False).
[03:16:52 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:52 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:53 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:53 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:53 TRACE The Living Valley] Player2 stream line: {"message":"\u003cCaroline\u003e That melody lingered—felt like the whole town held its breath, eh?","npc_id":"82daed6c-5128-4cff-bbe7-3868d3c9299e"}
[03:16:53 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:53 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:53 TRACE The Living Valley] Autonomy: Robin found target Jodi but out of talk range (dist=17.0).
[03:16:53 TRACE The Living Valley] Autonomy: Abigail found target Jodi but out of talk range (dist=6.0).
[03:16:53 TRACE The Living Valley] Autonomy: Alex found target Pam but out of talk range (dist=6.0).
[03:16:53 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=73.0).
[03:16:53 TRACE The Living Valley] Autonomy: Haley found target Marnie but out of talk range (dist=50.0).
[03:16:53 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=116.0).
[03:16:53 TRACE The Living Valley] Autonomy: Jodi found target Abigail but out of talk range (dist=6.0).
[03:16:53 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=32.0).
[03:16:53 TRACE The Living Valley] Autonomy: Marnie found target Alex but out of talk range (dist=26.0).
[03:16:53 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:53 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:16:53 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=166.0).
[03:16:53 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=6.0).
[03:16:53 TRACE The Living Valley] Autonomy: Penny found target Marnie but out of talk range (dist=59.0).
[03:16:53 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=32.0).
[03:16:53 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:53 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=166.0).
[03:16:53 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=169.0).
[03:16:53 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:16:53 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:16:53 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=349.0).
[03:16:53 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:53 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:53 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:53 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:16:53 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:53 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=116.0).
[03:16:53 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:16:53 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=73.0).
[03:16:53 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=193.0).
[03:16:53 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:53 TRACE The Living Valley] Player2 stream line: {"message":"\u003cLewis\u003e That Andy‑Susan stunt still has folks buzzing, especially about the fireworks safety.","npc_id":"7e91abd5-4e41-45b8-a516-911a9e2a134f"}
[03:16:54 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:54 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:54 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:54 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:54 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:55 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:55 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:55 TRACE The Living Valley] Player2 stream line: {"message":"\u003cEmily\u003e I heard some folks think the melody hinted at a secret we’ve yet to uncover.","npc_id":"93aedf85-550d-4129-921a-0c89057e1104"}
[03:16:55 TRACE The Living Valley] Player2 stream line: {"message":"\u003cGus\u003e We should reinforce safety checks before any more fireworks go off.","npc_id":"e21896dc-a2e5-40bd-a20e-561d8ce8491a"}
[03:16:55 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:55 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:55 DEBUG The Living Valley] Autonomy: Jodi->Pierre encounter approved! block=BaseAnchor location=SeedShop.
[03:16:55 DEBUG The Living Valley] Autonomy: Jodi->Pierre staged successfully, starting conversation.
[03:16:55 DEBUG The Living Valley] Autonomy: Jodi->Pierre Player2 encounter conversation launched (turns=4, continuation=False).
[03:16:56 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:56 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:56 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:56 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:56 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:56 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:57 TRACE The Living Valley] Player2 stream line: {"message":"\u003cLewis\u003e I'll draft stricter permits and have the town watch trained for the next show.","npc_id":"7e91abd5-4e41-45b8-a516-911a9e2a134f"}
[03:16:57 TRACE The Living Valley] Player2 stream line: {"message":"\u003cCaroline\u003e Well, I’ll back to the shop—keep an ear out for any clues, Emily.","npc_id":"82daed6c-5128-4cff-bbe7-3868d3c9299e"}
[03:16:57 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:57 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:57 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:57 TRACE The Living Valley] Player2 stream line: {"message":"\u003cJodi\u003e Did the Andy‑Susan stunt draw extra customers past the store doors, Pierre?","npc_id":"2cf2cdd3-dd20-4835-855f-60f15a291671"}
[03:16:57 DEBUG The Living Valley] Encounter conversation completed: Emily->Caroline enc=enc_18 turns=4/4 duration_ms=7256.
[03:16:57 TRACE The Living Valley] Encounter transcript T1 Emily->Caroline: Andy and Susan’s show left the town square humming, especially after that surprise song.
[03:16:57 TRACE The Living Valley] Encounter transcript T2 Caroline->Emily: That melody lingered—felt like the whole town held its breath, eh?
[03:16:57 TRACE The Living Valley] Encounter transcript T3 Emily->Caroline: I heard some folks think the melody hinted at a secret we’ve yet to uncover.
[03:16:57 TRACE The Living Valley] Encounter transcript T4 Caroline->Emily: Well, I’ll back to the shop—keep an ear out for any clues, Emily.
[03:16:57 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:57 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:57 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:58 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:58 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:58 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:58 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:58 TRACE The Living Valley] Player2 stream line: {"message":"\u003cGus\u003e Anyway, I'll check the supply ledger while you sort the permits.","npc_id":"e21896dc-a2e5-40bd-a20e-561d8ce8491a"}
[03:16:58 TRACE The Living Valley] Player2 stream line: {"message":"\u003cPierre\u003e The stunt brought a line, but most left before the lunch rush.","npc_id":"558669dc-ea40-4fe7-b10e-6e7e1c160a9e"}
[03:16:58 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:58 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:58 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:58 TRACE The Living Valley] Autonomy: Robin found target Abigail but out of talk range (dist=11.0).
[03:16:58 TRACE The Living Valley] Autonomy: Abigail found target Robin but out of talk range (dist=11.0).
[03:16:58 TRACE The Living Valley] Autonomy: Alex found target Pam but out of talk range (dist=16.0).
[03:16:58 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=82.0).
[03:16:58 TRACE The Living Valley] Autonomy: Haley found target Marnie but out of talk range (dist=55.0).
[03:16:58 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=114.0).
[03:16:58 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=42.0).
[03:16:58 TRACE The Living Valley] Autonomy: Marnie found target Alex but out of talk range (dist=26.0).
[03:16:58 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:16:58 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:16:58 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=175.0).
[03:16:58 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=16.0).
[03:16:58 TRACE The Living Valley] Autonomy: Penny found target Marnie but out of talk range (dist=59.0).
[03:16:58 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=42.0).
[03:16:58 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:16:58 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=175.0).
[03:16:58 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:16:58 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:16:58 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:16:58 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=358.0).
[03:16:58 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:16:58 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:16:58 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:16:58 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:16:58 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:16:58 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=114.0).
[03:16:58 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:16:58 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=82.0).
[03:16:58 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=202.0).
[03:16:58 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:16:58 DEBUG The Living Valley] Encounter conversation completed: Lewis->Gus enc=enc_19 turns=4/4 duration_ms=6548.
[03:16:58 TRACE The Living Valley] Encounter transcript T1 Lewis->Gus: That Andy‑Susan stunt still has folks buzzing, especially about the fireworks safety.
[03:16:58 TRACE The Living Valley] Encounter transcript T2 Gus->Lewis: We should reinforce safety checks before any more fireworks go off.
[03:16:58 TRACE The Living Valley] Encounter transcript T3 Lewis->Gus: I'll draft stricter permits and have the town watch trained for the next show.
[03:16:58 TRACE The Living Valley] Encounter transcript T4 Gus->Lewis: Anyway, I'll check the supply ledger while you sort the permits.
[03:16:59 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:59 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:59 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:16:59 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:16:59 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:59 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:16:59 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:00 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:00 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:00 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:00 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:00 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:00 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:00 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=2s)
[03:17:01 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:01 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:01 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:01 TRACE The Living Valley] Player2 stream line: {"message":"\u003cPierre\u003e Anyway, I’ll sort the shelves; we’ll cross that bridge if demand grows.","npc_id":"558669dc-ea40-4fe7-b10e-6e7e1c160a9e"}
[03:17:01 DEBUG The Living Valley] Encounter conversation completed: Jodi->Pierre enc=enc_20 turns=4/4 duration_ms=5780.
[03:17:01 TRACE The Living Valley] Encounter transcript T1 Jodi->Pierre: Did the Andy‑Susan stunt draw extra customers past the store doors, Pierre?
[03:17:01 TRACE The Living Valley] Encounter transcript T2 Pierre->Jodi: The stunt brought a line, but most left before the lunch rush.
[03:17:01 TRACE The Living Valley] Encounter transcript T3 Jodi->Pierre: If folks keep watching, we might need a bigger queue space soon.
[03:17:01 TRACE The Living Valley] Encounter transcript T4 Pierre->Jodi: Anyway, I’ll sort the shelves; we’ll cross that bridge if demand grows.
[03:17:01 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:01 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:01 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:02 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:02 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:02 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:02 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:02 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:02 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:03 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:03 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:03 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:03 DEBUG The Living Valley] Autonomy: Robin->Abigail encounter approved! block=BaseAnchor location=SeedShop.
[03:17:03 DEBUG The Living Valley] Autonomy: Robin->Abigail staged successfully, starting conversation.
[03:17:03 DEBUG The Living Valley] Autonomy: Robin->Abigail Player2 encounter conversation launched (turns=4, continuation=False).
[03:17:03 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:17:03 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:03 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:03 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:03 TRACE The Living Valley] Autonomy: Alex found target Pam but out of talk range (dist=25.0).
[03:17:03 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=91.0).
[03:17:03 TRACE The Living Valley] Autonomy: Haley found target Marnie but out of talk range (dist=55.0).
[03:17:03 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=105.0).
[03:17:03 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=51.0).
[03:17:03 TRACE The Living Valley] Autonomy: Marnie found target Alex but out of talk range (dist=26.0).
[03:17:03 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:17:03 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:03 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=185.0).
[03:17:03 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=25.0).
[03:17:03 TRACE The Living Valley] Autonomy: Penny found target Marnie but out of talk range (dist=59.0).
[03:17:03 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=51.0).
[03:17:03 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:03 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=185.0).
[03:17:03 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:03 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:03 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:03 TRACE The Living Valley] Autonomy: Beckett found target Anderson but out of talk range (dist=368.0).
[03:17:03 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:03 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:03 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:03 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:03 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:03 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=105.0).
[03:17:03 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:03 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=91.0).
[03:17:03 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=211.0).
[03:17:03 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:17:04 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:04 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:04 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:04 TRACE The Living Valley] Autonomy: encounter enc_21 Robin->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:04 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:04 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:04 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:04 TRACE The Living Valley] Autonomy: encounter enc_21 Robin->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:04 TRACE The Living Valley] Player2 stream line: {"message":"\u003cRobin\u003e Did you see Andy’s stunt with Susan’s fireworks by the square?","npc_id":"973709cb-a3de-4750-a18a-875eff02e2a4"}
[03:17:04 TRACE Farm Type Manager (FTM)] Spawned 1 objects. Time: 1230.
[03:17:05 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:05 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:05 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:05 TRACE The Living Valley] Autonomy: encounter enc_21 Robin->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:05 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:05 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:05 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:05 TRACE The Living Valley] Autonomy: encounter enc_21 Robin->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:05 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:17:06 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:06 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:06 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:06 TRACE The Living Valley] Autonomy: encounter enc_21 Robin->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:06 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:06 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:06 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:06 TRACE The Living Valley] Autonomy: encounter enc_21 Robin->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:06 TRACE The Living Valley] Player2 stream line: {"message":"\u003cAbigail\u003e Those fireworks were wild—maybe we should tighten safety checks for future events!","npc_id":"9a9a1df4-3b10-4e97-a2f6-6242ff416474"}
[03:17:07 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:07 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:07 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:07 TRACE The Living Valley] Autonomy: encounter enc_21 Robin->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:07 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:07 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:07 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:07 TRACE The Living Valley] Autonomy: encounter enc_21 Robin->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:07 TRACE SpaceCore] Event: BeforeWarp
[03:17:07 TRACE game] Warping to Town
[03:17:07 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:17:08 TRACE The Living Valley] Autonomy: encounter enc_18 Emily->Caroline waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:08 TRACE The Living Valley] Autonomy: encounter enc_19 Lewis->Gus waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:08 TRACE The Living Valley] Autonomy: encounter enc_20 Jodi->Pierre waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:08 TRACE The Living Valley] Autonomy: encounter enc_21 Robin->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:08 TRACE The Living Valley] Autonomy: cancelling encounter enc_18 Emily->Caroline (ui_interrupt).
[03:17:08 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Emily->Caroline after ui_interrupt.
[03:17:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Emily starting handoff: TilePoint=(21,14), controller=null, followSchedule=True, time=1230, map=SeedShop.
[03:17:08 TRACE The Living Valley] Autonomy: queued Emily for vanilla schedule resume after encounter enc_18 (ui_interrupt, restored=False, next_tick=23681, map=SeedShop, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Caroline starting handoff: TilePoint=(22,14), controller=null, followSchedule=True, time=1230, map=SeedShop.
[03:17:08 TRACE The Living Valley] Autonomy: queued Caroline for vanilla schedule resume after encounter enc_18 (ui_interrupt, restored=False, next_tick=23681, map=SeedShop, time=1230).
[03:17:08 TRACE The Living Valley] Autonomy: cancelling encounter enc_19 Lewis->Gus (ui_interrupt).
[03:17:08 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Lewis->Gus after ui_interrupt.
[03:17:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Lewis starting handoff: TilePoint=(49,62), controller=null, followSchedule=True, time=1230, map=Town.
[03:17:08 TRACE The Living Valley] Autonomy: queued Lewis for vanilla schedule resume after encounter enc_19 (ui_interrupt, restored=False, next_tick=23681, map=Town, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Gus starting handoff: TilePoint=(49,59), controller=null, followSchedule=True, time=1230, map=Town.
[03:17:08 TRACE The Living Valley] Autonomy: queued Gus for vanilla schedule resume after encounter enc_19 (ui_interrupt, restored=False, next_tick=23681, map=Town, time=1230).
[03:17:08 TRACE The Living Valley] Autonomy: cancelling encounter enc_20 Jodi->Pierre (ui_interrupt).
[03:17:08 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Jodi->Pierre after ui_interrupt.
[03:17:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Jodi starting handoff: TilePoint=(7,17), controller=null, followSchedule=True, time=1230, map=SeedShop.
[03:17:08 TRACE The Living Valley] Autonomy: queued Jodi for vanilla schedule resume after encounter enc_20 (ui_interrupt, restored=False, next_tick=23681, map=SeedShop, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Pierre starting handoff: TilePoint=(4,17), controller=null, followSchedule=True, time=1230, map=SeedShop.
[03:17:08 TRACE The Living Valley] Autonomy: queued Pierre for vanilla schedule resume after encounter enc_20 (ui_interrupt, restored=False, next_tick=23681, map=SeedShop, time=1230).
[03:17:08 TRACE The Living Valley] Autonomy: cancelling encounter enc_21 Robin->Abigail (ui_interrupt).
[03:17:08 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Robin->Abigail after ui_interrupt.
[03:17:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Robin starting handoff: TilePoint=(27,18), controller=null, followSchedule=True, time=1230, map=SeedShop.
[03:17:08 TRACE The Living Valley] Autonomy: queued Robin for vanilla schedule resume after encounter enc_21 (ui_interrupt, restored=False, next_tick=23681, map=SeedShop, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [HANDOFF] Abigail starting handoff: TilePoint=(26,20), controller=null, followSchedule=True, time=1230, map=SeedShop.
[03:17:08 TRACE The Living Valley] Autonomy: queued Abigail for vanilla schedule resume after encounter enc_21 (ui_interrupt, restored=False, next_tick=23681, map=SeedShop, time=1230).
[03:17:08 TRACE Content Patcher] Requested cache invalidation for all assets matching a predicate.
[03:17:08 TRACE SMAPI] Content Patcher edited Maps/winter_outdoorsTileSheet (for the 'Stardew Valley Expanded' content pack).
[03:17:08 TRACE SMAPI] Invalidated 1 asset names (Maps/winter_outdoorsTileSheet).
Propagated 1 core assets (Maps/winter_outdoorsTileSheet).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily starting rebind at TilePoint=(21,14), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1230.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily cleared schedule, calling TryLoadSchedule().
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily TryLoadSchedule returned=True, schedule_count=5, first_keys=900,1000,1300,1600,2430.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily current_time=1230, entries_before_current=900:HaleyHouse,1000:SeedShop.
[03:17:08 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Emily forced same-map active-slot path after encounter enc_18 (active_schedule_time=1000, next_schedule_time=1300, location=SeedShop, tile=(27,16), time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Emily reset complete: lastAttemptedSchedule=1230, previousEndPoint=(27,16), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1000, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(27,16), active_facing=2, active_behavior=none, fallback_used=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Gus starting rebind at TilePoint=(109,101), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1230.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Gus cleared schedule, calling TryLoadSchedule().
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Gus TryLoadSchedule returned=True, schedule_count=15, first_keys=700,930,950,1130,1400.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Gus current_time=1230, entries_before_current=700:Town,930:Town,950:Town,1130:Town.
[03:17:08 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Gus already at active-slot destination after encounter enc_19 (active_schedule_time=1130, next_schedule_time=1400, location=Town, tile=(109,101), time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Gus reset complete: lastAttemptedSchedule=1230, previousEndPoint=(109,101), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1130, next_schedule_time=1400, active_target_location=Town, active_target_tile=(109,101), active_facing=3, active_behavior=none, fallback_used=False.
[03:17:08 DEBUG The Living Valley] Autonomy: waiting to return Gus to vanilla schedule after encounter enc_19 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1230, active_schedule_time=1130, next_schedule_time=1400, active_target_location=Town, active_target_tile=(109,101), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(109,101), previousEndPoint=(109,101), lastAttemptedSchedule=1230, map=Town, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Caroline starting rebind at TilePoint=(22,14), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1230.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Caroline cleared schedule, calling TryLoadSchedule().
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Caroline TryLoadSchedule returned=True, schedule_count=6, first_keys=800,1030,1300,1600,1810.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Caroline current_time=1230, entries_before_current=800:SeedShop,1030:SeedShop.
[03:17:08 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Caroline forced same-map active-slot path after encounter enc_18 (active_schedule_time=1030, next_schedule_time=1300, location=SeedShop, tile=(24,17), time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Caroline reset complete: lastAttemptedSchedule=1230, previousEndPoint=(24,17), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1030, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(24,17), active_facing=3, active_behavior=none, fallback_used=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Lewis starting rebind at TilePoint=(49,62), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1230.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Lewis cleared schedule, calling TryLoadSchedule().
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Lewis TryLoadSchedule returned=True, schedule_count=7, first_keys=800,1000,1040,1140,1600.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Lewis current_time=1230, entries_before_current=800:ManorHouse,1000:Town,1040:Town,1140:SeedShop.
[03:17:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Lewis encounter=enc_19 from=Town to=SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30) arrival_resolved=True active_target_location=SeedShop active_target_tile=(6,19) time=1230.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Lewis reset complete: lastAttemptedSchedule=1230, previousEndPoint=(43,56), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1140, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(6,19), active_facing=0, active_behavior=none, fallback_used=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Jodi starting rebind at TilePoint=(7,17), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1230.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Jodi cleared schedule, calling TryLoadSchedule().
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Jodi TryLoadSchedule returned=True, schedule_count=8, first_keys=800,940,1000,1300,1600.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Jodi current_time=1230, entries_before_current=800:SamHouse,940:SamHouse,1000:SeedShop.
[03:17:08 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Jodi forced same-map active-slot path after encounter enc_20 (active_schedule_time=1000, next_schedule_time=1300, location=SeedShop, tile=(22,17), time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Jodi reset complete: lastAttemptedSchedule=1230, previousEndPoint=(22,17), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1000, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(22,17), active_facing=1, active_behavior=none, fallback_used=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Pierre starting rebind at TilePoint=(4,17), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1230.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Pierre cleared schedule, calling TryLoadSchedule().
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Pierre TryLoadSchedule returned=True, schedule_count=5, first_keys=700,830,1700,1800,2030.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Pierre current_time=1230, entries_before_current=700:SeedShop,830:SeedShop.
[03:17:08 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Pierre already at active-slot destination after encounter enc_20 (active_schedule_time=830, next_schedule_time=1700, location=SeedShop, tile=(4,17), time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Pierre reset complete: lastAttemptedSchedule=1230, previousEndPoint=(4,17), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=830, next_schedule_time=1700, active_target_location=SeedShop, active_target_tile=(4,17), active_facing=2, active_behavior=none, fallback_used=False.
[03:17:08 DEBUG The Living Valley] Autonomy: waiting to return Pierre to vanilla schedule after encounter enc_20 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1230, active_schedule_time=830, next_schedule_time=1700, active_target_location=SeedShop, active_target_tile=(4,17), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(4,17), previousEndPoint=(4,17), lastAttemptedSchedule=1230, map=SeedShop, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Robin starting rebind at TilePoint=(27,18), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1230.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Robin cleared schedule, calling TryLoadSchedule().
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Robin TryLoadSchedule returned=True, schedule_count=5, first_keys=930,1300,1600,1800,2100.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Robin current_time=1230, entries_before_current=930:SeedShop.
[03:17:08 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Robin already at active-slot destination after encounter enc_21 (active_schedule_time=930, next_schedule_time=1300, location=SeedShop, tile=(27,18), time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Robin reset complete: lastAttemptedSchedule=1230, previousEndPoint=(27,18), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=930, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(27,18), active_facing=0, active_behavior=none, fallback_used=False.
[03:17:08 DEBUG The Living Valley] Autonomy: waiting to return Robin to vanilla schedule after encounter enc_21 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1230, active_schedule_time=930, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(27,18), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(27,18), previousEndPoint=(27,18), lastAttemptedSchedule=1230, map=SeedShop, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Abigail starting rebind at TilePoint=(26,20), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1230.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Abigail cleared schedule, calling TryLoadSchedule().
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Abigail TryLoadSchedule returned=True, schedule_count=5, first_keys=900,1030,1300,1630,1930.
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Abigail current_time=1230, entries_before_current=900:SeedShop,1030:SeedShop.
[03:17:08 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Abigail forced same-map active-slot path after encounter enc_21 (active_schedule_time=1030, next_schedule_time=1300, location=SeedShop, tile=(2,20), time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [REBIND] Abigail reset complete: lastAttemptedSchedule=1230, previousEndPoint=(2,20), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1030, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(2,20), active_facing=3, active_behavior=none, fallback_used=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [ARRIVAL] Gus active-slot handoff at tile (109,101) in Town (active_schedule_time=1130, active_facing=3, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(109,101), facing=2, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: returned Gus to active-slot schedule action after encounter enc_19 (ui_interrupt, restored=False, attempts=1, active_schedule_time=1130, next_schedule_time=1400, active_target_location=Town, active_target_tile=(109,101), active_facing=3, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(109,101), previousEndPoint=(109,101), lastAttemptedSchedule=1230, map=Town, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [ARRIVAL] Pierre active-slot handoff at tile (4,17) in SeedShop (active_schedule_time=830, active_facing=2, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(4,17), facing=2, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: returned Pierre to active-slot schedule action after encounter enc_20 (ui_interrupt, restored=False, attempts=1, active_schedule_time=830, next_schedule_time=1700, active_target_location=SeedShop, active_target_tile=(4,17), active_facing=2, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(4,17), previousEndPoint=(4,17), lastAttemptedSchedule=1230, map=SeedShop, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [ARRIVAL] Robin active-slot handoff at tile (27,18) in SeedShop (active_schedule_time=930, active_facing=0, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(27,18), facing=0, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: returned Robin to active-slot schedule action after encounter enc_21 (ui_interrupt, restored=False, attempts=1, active_schedule_time=930, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(27,18), active_facing=0, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(27,18), previousEndPoint=(27,18), lastAttemptedSchedule=1230, map=SeedShop, time=1230).
[03:17:08 DEBUG The Living Valley] Autonomy: [MONITOR] Pierre encounter=enc_20 tick=1: controller=PathFindController, isMoving=False, TilePoint=(4,17), moved_from_initial=no, previousEndPoint=(4,17), followSchedule=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [MONITOR] Gus encounter=enc_19 tick=1: controller=PathFindController, isMoving=False, TilePoint=(109,101), moved_from_initial=no, previousEndPoint=(109,101), followSchedule=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_21 tick=1: controller=PathFindController, isMoving=False, TilePoint=(27,18), moved_from_initial=no, previousEndPoint=(27,18), followSchedule=True.
[03:17:08 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:08 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=99.0).
[03:17:08 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=23.0).
[03:17:08 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=97.0).
[03:17:08 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=59.0).
[03:17:08 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:08 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=13.0).
[03:17:08 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:08 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=197.0).
[03:17:08 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=32.0).
[03:17:08 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:08 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=59.0).
[03:17:08 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:08 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=197.0).
[03:17:08 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:08 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:08 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:08 TRACE The Living Valley] Autonomy: Beckett found target Marlon but out of talk range (dist=13.0).
[03:17:08 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:08 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:08 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:08 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:08 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:08 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=97.0).
[03:17:08 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:08 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=99.0).
[03:17:08 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=219.0).
[03:17:08 TRACE The Living Valley] Player2 stream line: {"message":"\u003cRobin\u003e Agreed—I'll draft stricter permits; town safety can't be an afterthought.","npc_id":"973709cb-a3de-4750-a18a-875eff02e2a4"}
[03:17:08 DEBUG The Living Valley] Autonomy: [MONITOR] Pierre encounter=enc_20 tick=2: controller=PathFindController, isMoving=False, TilePoint=(4,17), moved_from_initial=no, previousEndPoint=(4,17), followSchedule=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [MONITOR] Gus encounter=enc_19 tick=2: controller=PathFindController, isMoving=False, TilePoint=(109,101), moved_from_initial=no, previousEndPoint=(109,101), followSchedule=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_21 tick=2: controller=PathFindController, isMoving=False, TilePoint=(27,18), moved_from_initial=no, previousEndPoint=(27,18), followSchedule=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [MONITOR] Pierre encounter=enc_20 tick=3: controller=PathFindController, isMoving=False, TilePoint=(4,17), moved_from_initial=no, previousEndPoint=(4,17), followSchedule=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [MONITOR] Gus encounter=enc_19 tick=3: controller=PathFindController, isMoving=False, TilePoint=(109,101), moved_from_initial=no, previousEndPoint=(109,101), followSchedule=True.
[03:17:08 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_21 tick=3: controller=PathFindController, isMoving=False, TilePoint=(27,18), moved_from_initial=no, previousEndPoint=(27,18), followSchedule=True.
[03:17:09 DEBUG The Living Valley] Autonomy: [MONITOR] Pierre encounter=enc_20 tick=4: controller=PathFindController, isMoving=False, TilePoint=(4,17), moved_from_initial=no, previousEndPoint=(4,17), followSchedule=True.
[03:17:09 DEBUG The Living Valley] Autonomy: [MONITOR] Gus encounter=enc_19 tick=4: controller=PathFindController, isMoving=False, TilePoint=(109,101), moved_from_initial=no, previousEndPoint=(109,101), followSchedule=True.
[03:17:09 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_21 tick=4: controller=PathFindController, isMoving=False, TilePoint=(27,18), moved_from_initial=no, previousEndPoint=(27,18), followSchedule=True.
[03:17:09 DEBUG The Living Valley] Autonomy: [MONITOR] Pierre encounter=enc_20 tick=5: controller=PathFindController, isMoving=False, TilePoint=(4,17), moved_from_initial=no, previousEndPoint=(4,17), followSchedule=True.
[03:17:09 DEBUG The Living Valley] Autonomy: [MONITOR] Gus encounter=enc_19 tick=5: controller=PathFindController, isMoving=False, TilePoint=(109,101), moved_from_initial=no, previousEndPoint=(109,101), followSchedule=True.
[03:17:09 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_21 tick=5: controller=PathFindController, isMoving=False, TilePoint=(27,18), moved_from_initial=no, previousEndPoint=(27,18), followSchedule=True.
[03:17:09 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=2s)
[03:17:10 TRACE The Living Valley] Player2 stream line: {"message":"\u003cAbigail\u003e Anyway, back to the seed shop—let's keep those plans growing!","npc_id":"9a9a1df4-3b10-4e97-a2f6-6242ff416474"}
[03:17:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(stale)] Lewis encounter=enc_19 leg=Town->SeedShop tile=(49,62) map=Town retry_count=0.
[03:17:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Lewis encounter=enc_19 from=Town to=SeedShop transition_tile=(43,56) approach_tile=(43,56) arrival_tile=(6,30) arrival_resolved=True active_target_location=SeedShop active_target_tile=(6,19) time=1230.
[03:17:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(retry)] Lewis encounter=enc_19 restarted leg toward SeedShop from Town using transition_tile=(43,56) approach_tile=(43,56) retry_count=1.
[03:17:11 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=2s)
[03:17:11 TRACE SpaceCore] Event: OnEventFinished
[03:17:11 TRACE SpaceCore] Event: BeforeWarp
[03:17:11 TRACE game] Warping to Town
[03:17:11 TRACE SMAPI] Content Patcher loaded asset 'Characters/Dusty' (for the 'Stardew Valley Expanded' content pack).
[03:17:11 TRACE SMAPI] Content Patcher loaded asset 'Portraits/Dusty' (for the 'Stardew Valley Expanded' content pack).
[03:17:13 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:13 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=99.0).
[03:17:13 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=23.0).
[03:17:13 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=97.0).
[03:17:13 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=59.0).
[03:17:13 TRACE The Living Valley] Autonomy: Leah found target Morrow but out of talk range (dist=40.0).
[03:17:13 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:13 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=13.0).
[03:17:13 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:13 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=197.0).
[03:17:13 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=32.0).
[03:17:13 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:13 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=59.0).
[03:17:13 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:13 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=197.0).
[03:17:13 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:13 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:13 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:13 TRACE The Living Valley] Autonomy: Beckett found target Marlon but out of talk range (dist=13.0).
[03:17:13 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:13 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:13 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:13 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:13 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:13 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=97.0).
[03:17:13 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:13 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:13 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=99.0).
[03:17:13 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=219.0).
[03:17:14 TRACE SpaceCore] Event: OnEventFinished
[03:17:14 TRACE SpaceCore] Event: BeforeWarp
[03:17:14 TRACE game] Warping to Town
[03:17:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(stale)] Lewis encounter=enc_19 leg=Town->SeedShop tile=(49,62) map=Town retry_count=1.
[03:17:14 WARN  The Living Valley] Autonomy: [CrossMapLeg(stale)] Lewis encounter=enc_19 exceeded retry limit for leg Town->SeedShop; clearing fallback until next vanilla schedule boundary.
[03:17:15 TRACE SpaceCore] Event: OnEventFinished
[03:17:15 TRACE SpaceCore] Event: BeforeWarp
[03:17:15 TRACE game] Warping to Town
[03:17:17 TRACE SpaceCore] Event: OnEventFinished
[03:17:17 TRACE SpaceCore] Event: BeforeWarp
[03:17:17 TRACE game] Warping to Town
[03:17:18 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:18 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=99.0).
[03:17:18 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=23.0).
[03:17:18 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=97.0).
[03:17:18 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=59.0).
[03:17:18 TRACE The Living Valley] Autonomy: Leah found target Morrow but out of talk range (dist=40.0).
[03:17:18 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:18 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=13.0).
[03:17:18 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:18 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=197.0).
[03:17:18 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=32.0).
[03:17:18 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:18 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=59.0).
[03:17:18 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:18 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=197.0).
[03:17:18 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:18 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:18 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:18 TRACE The Living Valley] Autonomy: Beckett found target Marlon but out of talk range (dist=13.0).
[03:17:18 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:18 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:18 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:18 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:18 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:18 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=97.0).
[03:17:18 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:18 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:18 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=99.0).
[03:17:18 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=219.0).
[03:17:19 TRACE SpaceCore] Event: OnEventFinished
[03:17:19 TRACE SpaceCore] Event: BeforeWarp
[03:17:19 TRACE game] Warping to Town
[03:17:21 TRACE SpaceCore] Event: OnEventFinished
[03:17:21 TRACE SpaceCore] Event: BeforeWarp
[03:17:21 TRACE game] Warping to Town
[03:17:23 TRACE SpaceCore] Event: OnEventFinished
[03:17:23 TRACE SpaceCore] Event: BeforeWarp
[03:17:23 TRACE game] Warping to Town
[03:17:23 TRACE The Living Valley] Ambient command unlocks day 121: adjust_town_sentiment | events=6 public=3 market=0 scarcity=0 oversupply=0 anomaly=False
[03:17:23 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:23 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=99.0).
[03:17:23 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=23.0).
[03:17:23 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=97.0).
[03:17:23 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=59.0).
[03:17:23 TRACE The Living Valley] Autonomy: Leah found target Morrow but out of talk range (dist=40.0).
[03:17:23 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:23 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=13.0).
[03:17:23 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:23 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=197.0).
[03:17:23 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=32.0).
[03:17:23 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:23 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=59.0).
[03:17:23 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:23 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=197.0).
[03:17:23 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:23 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:23 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:23 TRACE The Living Valley] Autonomy: Beckett found target Marlon but out of talk range (dist=13.0).
[03:17:23 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:23 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:23 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:23 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:23 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:23 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=97.0).
[03:17:23 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:23 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:23 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=99.0).
[03:17:23 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=219.0).
[03:17:25 TRACE SpaceCore] Event: OnEventFinished
[03:17:25 TRACE SpaceCore] Event: BeforeWarp
[03:17:25 TRACE game] Warping to Town
[03:17:27 DEBUG The Living Valley] Autonomy: Gunther->GuntherSilvian encounter approved! block=ReturnHome location=ArchaeologyHouse.
[03:17:27 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian blocked by wall (no line of sight).
[03:17:27 TRACE SpaceCore] Event: OnEventFinished
[03:17:27 TRACE SpaceCore] Event: BeforeWarp
[03:17:27 TRACE game] Warping to Town
[03:17:28 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:28 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=99.0).
[03:17:28 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=23.0).
[03:17:28 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=97.0).
[03:17:28 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=59.0).
[03:17:28 TRACE The Living Valley] Autonomy: Leah found target Morrow but out of talk range (dist=40.0).
[03:17:28 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:28 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=13.0).
[03:17:28 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:28 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=197.0).
[03:17:28 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=32.0).
[03:17:28 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:28 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=59.0).
[03:17:28 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:28 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=197.0).
[03:17:28 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:28 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:28 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:28 TRACE The Living Valley] Autonomy: Beckett found target Marlon but out of talk range (dist=13.0).
[03:17:28 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:28 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:28 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:28 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:28 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:28 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=97.0).
[03:17:28 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:28 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:28 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=99.0).
[03:17:28 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=219.0).
[03:17:31 TRACE SpaceCore] Event: OnEventFinished
[03:17:31 TRACE SpaceCore] Event: BeforeWarp
[03:17:31 TRACE game] Warping to Town
[03:17:31 TRACE SMAPI] Content Patcher loaded asset 'Characters/Dusty' (for the 'Stardew Valley Expanded' content pack).
[03:17:31 TRACE SMAPI] Content Patcher loaded asset 'Portraits/Dusty' (for the 'Stardew Valley Expanded' content pack).
[03:17:33 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:33 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=99.0).
[03:17:33 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=23.0).
[03:17:33 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=97.0).
[03:17:33 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=59.0).
[03:17:33 TRACE The Living Valley] Autonomy: Leah found target Morrow but out of talk range (dist=40.0).
[03:17:33 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:33 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=13.0).
[03:17:33 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:33 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=197.0).
[03:17:33 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=32.0).
[03:17:33 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:33 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=59.0).
[03:17:33 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:33 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=197.0).
[03:17:33 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:33 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:33 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:33 TRACE The Living Valley] Autonomy: Beckett found target Marlon but out of talk range (dist=13.0).
[03:17:33 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:33 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:33 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:33 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:33 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:33 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=97.0).
[03:17:33 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:33 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:33 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=99.0).
[03:17:33 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=219.0).
[03:17:33 TRACE SpaceCore] Event: OnEventFinished
[03:17:33 TRACE SpaceCore] Event: BeforeWarp
[03:17:33 TRACE game] Warping to Town
[03:17:36 DEBUG The Living Valley] Autonomy: Caroline->Robin encounter approved! block=BaseAnchor location=SeedShop.
[03:17:36 DEBUG The Living Valley] Autonomy: Caroline->Robin staged successfully, starting conversation.
[03:17:36 DEBUG The Living Valley] Autonomy: Caroline->Robin Player2 encounter conversation launched (turns=4, continuation=False).
[03:17:37 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:37 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:37 TRACE The Living Valley] Player2 stream line: {"message":"\u003cCaroline\u003e Did you see Olivia and the Wizard at the festival? Their magic drew quite the crowd.","npc_id":"82daed6c-5128-4cff-bbe7-3868d3c9299e"}
[03:17:38 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:38 DEBUG The Living Valley] Autonomy: [ARRIVAL] Emily active-slot handoff at tile (27,16) in SeedShop (active_schedule_time=1000, active_facing=2, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(27,15), facing=2, time=1230).
[03:17:38 DEBUG The Living Valley] Autonomy: returned Emily to active-slot schedule action after encounter enc_18 (ui_interrupt, restored=False, attempts=1, active_schedule_time=1000, next_schedule_time=1300, active_target_location=SeedShop, active_target_tile=(27,16), active_facing=2, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(27,15), previousEndPoint=(27,16), lastAttemptedSchedule=1230, map=SeedShop, time=1230).
[03:17:38 DEBUG The Living Valley] Autonomy: [MONITOR] Emily encounter=enc_18 tick=1: controller=PathFindController, isMoving=True, TilePoint=(27,15), moved_from_initial=yes, previousEndPoint=(27,16), followSchedule=True.
[03:17:38 DEBUG The Living Valley] Autonomy: [MONITOR] Emily encounter=enc_18 tick=2: controller=PathFindController, isMoving=True, TilePoint=(27,15), moved_from_initial=yes, previousEndPoint=(27,16), followSchedule=True.
[03:17:38 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:38 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=9.0).
[03:17:38 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:38 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=107.0).
[03:17:38 TRACE The Living Valley] Autonomy: Emily found target Abigail but out of talk range (dist=10.0).
[03:17:38 TRACE The Living Valley] Autonomy: Gus found target Pam but out of talk range (dist=64.0).
[03:17:38 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=23.0).
[03:17:38 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=90.0).
[03:17:38 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=67.0).
[03:17:38 TRACE The Living Valley] Autonomy: Leah found target Morrow but out of talk range (dist=40.0).
[03:17:38 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:38 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=13.0).
[03:17:38 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:38 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=211.0).
[03:17:38 TRACE The Living Valley] Autonomy: Pam found target Alex but out of talk range (dist=40.0).
[03:17:38 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:38 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=67.0).
[03:17:38 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:38 TRACE The Living Valley] Autonomy: Shane found target MorrisTod but out of talk range (dist=211.0).
[03:17:38 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:38 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:38 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:38 TRACE The Living Valley] Autonomy: Beckett found target Marlon but out of talk range (dist=13.0).
[03:17:38 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:38 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:38 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:38 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:38 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:38 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=90.0).
[03:17:38 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:38 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:38 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=107.0).
[03:17:38 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=227.0).
[03:17:38 DEBUG The Living Valley] Autonomy: [MONITOR] Emily encounter=enc_18 tick=3: controller=PathFindController, isMoving=True, TilePoint=(27,15), moved_from_initial=yes, previousEndPoint=(27,16), followSchedule=True.
[03:17:38 DEBUG The Living Valley] Autonomy: [MONITOR] Emily encounter=enc_18 tick=4: controller=PathFindController, isMoving=True, TilePoint=(27,16), moved_from_initial=yes, previousEndPoint=(27,16), followSchedule=True.
[03:17:39 DEBUG The Living Valley] Autonomy: [MONITOR] Emily encounter=enc_18 tick=5: controller=PathFindController, isMoving=True, TilePoint=(27,16), moved_from_initial=yes, previousEndPoint=(27,16), followSchedule=True.
[03:17:39 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:39 TRACE The Living Valley] Player2 stream line: {"message":"\u003cRobin\u003e Their spell was impressive, but the lanterns kept falling—needs better setup.","npc_id":"973709cb-a3de-4750-a18a-875eff02e2a4"}
[03:17:39 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:39 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:17:40 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:40 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:41 TRACE The Living Valley] Player2 stream line: {"message":"\u003cCaroline\u003e Maybe we should help reinforce the lantern posts before the next event.","npc_id":"82daed6c-5128-4cff-bbe7-3868d3c9299e"}
[03:17:41 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:41 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:41 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:17:42 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:42 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:43 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:43 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:43 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=9.0).
[03:17:43 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:43 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=116.0).
[03:17:43 TRACE The Living Valley] Autonomy: Emily found target Abigail but out of talk range (dist=23.0).
[03:17:43 TRACE The Living Valley] Autonomy: Gus found target Alex but out of talk range (dist=80.0).
[03:17:43 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=18.0).
[03:17:43 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=89.0).
[03:17:43 DEBUG The Living Valley] Autonomy: Jodi->Abigail encounter approved! block=BaseAnchor location=SeedShop.
[03:17:43 DEBUG The Living Valley] Autonomy: Jodi->Abigail staged successfully, starting conversation.
[03:17:43 DEBUG The Living Valley] Autonomy: Jodi->Abigail Player2 encounter conversation launched (turns=4, continuation=False).
[03:17:43 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=76.0).
[03:17:43 TRACE The Living Valley] Autonomy: Leah found target Morrow but out of talk range (dist=40.0).
[03:17:43 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:43 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=22.0).
[03:17:43 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:43 TRACE The Living Valley] Autonomy: MorrisTod found target Pam but out of talk range (dist=201.0).
[03:17:43 TRACE The Living Valley] Autonomy: Pam found target Shane but out of talk range (dist=19.0).
[03:17:43 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:43 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=76.0).
[03:17:43 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:43 TRACE The Living Valley] Autonomy: Shane found target Pam but out of talk range (dist=19.0).
[03:17:43 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:43 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:43 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:43 TRACE The Living Valley] Autonomy: Beckett found target Haley but out of talk range (dist=18.0).
[03:17:43 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:43 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:43 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:43 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:43 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:43 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=89.0).
[03:17:43 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:43 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:43 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=116.0).
[03:17:43 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=236.0).
[03:17:44 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:44 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:44 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:45 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:45 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:45 TRACE The Living Valley] Player2 stream line: {"message":"\u003cJodi\u003e Did you see Olivia and the Wizard setting up that odd lantern display, Abigail?","npc_id":"2cf2cdd3-dd20-4835-855f-60f15a291671"}
[03:17:45 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:45 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:46 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:46 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:46 TRACE Farm Type Manager (FTM)] Spawned 1 objects. Time: 1250.
[03:17:46 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:17:46 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:46 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:47 TRACE The Living Valley] Player2 stream line: {"message":"\u003cAbigail\u003e It looked like a warning sign—maybe something's brewing beyond the farm.","npc_id":"9a9a1df4-3b10-4e97-a2f6-6242ff416474"}
[03:17:47 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:47 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:47 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:47 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:48 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=2s)
[03:17:48 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:48 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:48 TRACE The Living Valley] Player2 stream line: {"message":"\u003cJodi\u003e Whispers say they’re prepping for a new town tradition, but nobody’s sure yet.","npc_id":"2cf2cdd3-dd20-4835-855f-60f15a291671"}
[03:17:48 TRACE SpaceCore] Event: BeforeWarp
[03:17:48 TRACE game] Warping to BusStop
[03:17:48 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:48 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:48 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=9.0).
[03:17:48 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:48 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=126.0).
[03:17:48 TRACE The Living Valley] Autonomy: Emily found target Pierre but out of talk range (dist=40.0).
[03:17:48 TRACE The Living Valley] Autonomy: Gus found target Alex but out of talk range (dist=80.0).
[03:17:48 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=11.0).
[03:17:48 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=88.0).
[03:17:48 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=85.0).
[03:17:48 TRACE The Living Valley] Autonomy: Leah found target Morrow but out of talk range (dist=40.0).
[03:17:48 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:48 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=19.0).
[03:17:48 TRACE The Living Valley] Autonomy: Maru found target Willy but out of talk range (dist=7.0).
[03:17:48 TRACE The Living Valley] Autonomy: MorrisTod found target Pam but out of talk range (dist=217.0).
[03:17:48 TRACE The Living Valley] Autonomy: Pam found target Shane but out of talk range (dist=10.0).
[03:17:48 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:48 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=85.0).
[03:17:48 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:48 TRACE The Living Valley] Autonomy: Shane found target Pam but out of talk range (dist=10.0).
[03:17:48 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=169.0).
[03:17:48 TRACE The Living Valley] Autonomy: Willy found target Maru but out of talk range (dist=7.0).
[03:17:48 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:48 TRACE The Living Valley] Autonomy: Beckett found target Haley but out of talk range (dist=11.0).
[03:17:48 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:48 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:48 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:48 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:48 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:48 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=88.0).
[03:17:48 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:48 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:48 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=126.0).
[03:17:48 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=245.0).
[03:17:49 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:49 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:49 TRACE Content Patcher] Requested cache invalidation for all assets matching a predicate.
[03:17:49 TRACE SMAPI] Content Patcher edited LooseSprites/font_bold (for the 'Stardew Valley Expanded' content pack).
[03:17:49 TRACE SMAPI] Invalidated 1 asset names (LooseSprites/font_bold).
Propagated 1 core assets (LooseSprites/font_bold).
[03:17:49 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:49 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:50 TRACE The Living Valley] Player2 stream line: {"message":"\u003cRobin\u003e Anyway, I’ll order extra wood; we’ll get those posts sturdy before the next show.","npc_id":"973709cb-a3de-4750-a18a-875eff02e2a4"}
[03:17:50 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:50 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:50 TRACE The Living Valley] Player2 stream line: {"message":"\u003cAbigail\u003e Anyway, back to the seed shop—let’s keep an eye on those mysterious plans!","npc_id":"9a9a1df4-3b10-4e97-a2f6-6242ff416474"}
[03:17:50 DEBUG The Living Valley] Encounter conversation completed: Jodi->Abigail enc=enc_23 turns=4/4 duration_ms=6700.
[03:17:50 TRACE The Living Valley] Encounter transcript T1 Jodi->Abigail: Did you see Olivia and the Wizard setting up that odd lantern display, Abigail?
[03:17:50 TRACE The Living Valley] Encounter transcript T2 Abigail->Jodi: It looked like a warning sign—maybe something's brewing beyond the farm.
[03:17:50 TRACE The Living Valley] Encounter transcript T3 Jodi->Abigail: Whispers say they’re prepping for a new town tradition, but nobody’s sure yet.
[03:17:50 TRACE The Living Valley] Encounter transcript T4 Abigail->Jodi: Anyway, back to the seed shop—let’s keep an eye on those mysterious plans!
[03:17:50 DEBUG The Living Valley] Encounter conversation completed: Caroline->Robin enc=enc_22 turns=4/4 duration_ms=14359.
[03:17:50 TRACE The Living Valley] Encounter transcript T1 Caroline->Robin: Did you see Olivia and the Wizard at the festival? Their magic drew quite the crowd.
[03:17:50 TRACE The Living Valley] Encounter transcript T2 Robin->Caroline: Their spell was impressive, but the lanterns kept falling—needs better setup.
[03:17:50 TRACE The Living Valley] Encounter transcript T3 Caroline->Robin: Maybe we should help reinforce the lantern posts before the next event.
[03:17:50 TRACE The Living Valley] Encounter transcript T4 Robin->Caroline: Anyway, I’ll order extra wood; we’ll get those posts sturdy before the next show.
[03:17:50 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:17:50 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:50 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:51 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:51 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:51 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:51 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:52 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:52 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:52 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:52 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:53 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:53 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:53 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:53 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:53 TRACE The Living Valley] Autonomy: Lewis found target Marnie but out of talk range (dist=9.0).
[03:17:53 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=17.0).
[03:17:53 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=132.0).
[03:17:53 TRACE The Living Valley] Autonomy: Emily found target Pierre but out of talk range (dist=47.0).
[03:17:53 TRACE The Living Valley] Autonomy: Gus found target Alex but out of talk range (dist=80.0).
[03:17:53 TRACE The Living Valley] Autonomy: Haley found target Beckett but out of talk range (dist=9.0).
[03:17:53 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=87.0).
[03:17:53 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=92.0).
[03:17:53 TRACE The Living Valley] Autonomy: Leah found target Morrow but out of talk range (dist=40.0).
[03:17:53 TRACE The Living Valley] Autonomy: Marnie found target Lewis but out of talk range (dist=9.0).
[03:17:53 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=17.0).
[03:17:53 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=7.0).
[03:17:53 TRACE The Living Valley] Autonomy: MorrisTod found target Pam but out of talk range (dist=227.0).
[03:17:53 TRACE The Living Valley] Autonomy: Pam found target Shane but out of talk range (dist=7.0).
[03:17:53 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:53 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=92.0).
[03:17:53 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:53 TRACE The Living Valley] Autonomy: Shane found target Pam but out of talk range (dist=7.0).
[03:17:53 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:53 TRACE The Living Valley] Autonomy: Willy found target Harvey but out of talk range (dist=8.0).
[03:17:53 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:53 TRACE The Living Valley] Autonomy: Beckett found target Haley but out of talk range (dist=9.0).
[03:17:53 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:53 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:53 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:53 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:53 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:53 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=87.0).
[03:17:53 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:53 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:53 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=132.0).
[03:17:53 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=252.0).
[03:17:54 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:54 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:54 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:54 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:54 DEBUG The Living Valley] Autonomy: [REBIND] Caroline reset complete: lastAttemptedSchedule=1300, previousEndPoint=(24,21), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(24,21), active_facing=0, active_behavior=caroline_exercise, fallback_used=False.
[03:17:54 DEBUG The Living Valley] Autonomy: returned Caroline to vanilla schedule after encounter enc_18 (ui_interrupt, restored=False, attempts=2, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1300, active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(24,21), fallback_used=False, resumed=true, method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(24,15), previousEndPoint=(24,21), lastAttemptedSchedule=1300, map=SeedShop, time=1300).
[03:17:54 DEBUG The Living Valley] Autonomy: returned Leah to vanilla schedule after encounter enc_17 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1200, active_schedule_time=1030, next_schedule_time=1300, active_target_location=Downhill, active_target_tile=(53,43), fallback_used=False, resumed=true, method=VanillaSchedule(update), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(42,23), previousEndPoint=(67,67), lastAttemptedSchedule=1300, map=BusStop, time=1300).
[03:17:54 DEBUG The Living Valley] Autonomy: [REBIND] Jodi reset complete: lastAttemptedSchedule=1300, previousEndPoint=(21,17), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(21,17), active_facing=2, active_behavior=jodi_exercise, fallback_used=False.
[03:17:54 DEBUG The Living Valley] Autonomy: returned Jodi to vanilla schedule after encounter enc_20 (ui_interrupt, restored=False, attempts=2, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1300, active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(21,17), fallback_used=False, resumed=true, method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(15,12), previousEndPoint=(21,17), lastAttemptedSchedule=1300, map=SeedShop, time=1300).
[03:17:54 DEBUG The Living Valley] Autonomy: [REBIND] Abigail reset complete: lastAttemptedSchedule=1300, previousEndPoint=(73,54), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1300, next_schedule_time=1630, active_target_location=Town, active_target_tile=(73,54), active_facing=2, active_behavior=none, fallback_used=False.
[03:17:54 DEBUG The Living Valley] Autonomy: returned Abigail to vanilla schedule after encounter enc_21 (ui_interrupt, restored=False, attempts=2, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1300, active_schedule_time=1300, next_schedule_time=1630, active_target_location=Town, active_target_tile=(73,54), fallback_used=False, resumed=true, method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(17,12), previousEndPoint=(73,54), lastAttemptedSchedule=1300, map=SeedShop, time=1300).
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_20 tick=1: controller=null, isMoving=False, TilePoint=(15,12), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Leah encounter=enc_17 tick=1: controller=PathFindController, isMoving=True, TilePoint=(42,23), moved_from_initial=no, previousEndPoint=(67,67), followSchedule=True.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_18 tick=1: controller=null, isMoving=False, TilePoint=(24,15), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_21 tick=1: controller=null, isMoving=False, TilePoint=(17,12), moved_from_initial=yes, previousEndPoint=(73,54), followSchedule=False.
[03:17:55 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:55 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_20 tick=2: controller=null, isMoving=False, TilePoint=(15,12), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Leah encounter=enc_17 tick=2: controller=PathFindController, isMoving=True, TilePoint=(43,23), moved_from_initial=yes, previousEndPoint=(67,67), followSchedule=True.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_18 tick=2: controller=null, isMoving=False, TilePoint=(24,15), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_21 tick=2: controller=null, isMoving=False, TilePoint=(17,12), moved_from_initial=yes, previousEndPoint=(73,54), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_20 tick=3: controller=null, isMoving=False, TilePoint=(15,12), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Leah encounter=enc_17 tick=3: controller=PathFindController, isMoving=True, TilePoint=(43,23), moved_from_initial=yes, previousEndPoint=(67,67), followSchedule=True.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_18 tick=3: controller=null, isMoving=False, TilePoint=(24,15), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_21 tick=3: controller=null, isMoving=False, TilePoint=(17,12), moved_from_initial=yes, previousEndPoint=(73,54), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_20 tick=4: controller=null, isMoving=False, TilePoint=(15,12), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Leah encounter=enc_17 tick=4: controller=null, isMoving=True, TilePoint=(0,54), moved_from_initial=yes, previousEndPoint=(67,67), followSchedule=True.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_18 tick=4: controller=null, isMoving=False, TilePoint=(24,15), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_21 tick=4: controller=null, isMoving=False, TilePoint=(17,12), moved_from_initial=yes, previousEndPoint=(73,54), followSchedule=False.
[03:17:55 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:55 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_20 tick=5: controller=null, isMoving=False, TilePoint=(15,12), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Leah encounter=enc_17 tick=5: controller=null, isMoving=True, TilePoint=(0,54), moved_from_initial=yes, previousEndPoint=(67,67), followSchedule=True.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_18 tick=5: controller=null, isMoving=False, TilePoint=(24,15), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=False.
[03:17:55 DEBUG The Living Valley] Autonomy: [MONITOR] Abigail encounter=enc_21 tick=5: controller=null, isMoving=False, TilePoint=(17,12), moved_from_initial=yes, previousEndPoint=(73,54), followSchedule=False.
[03:17:56 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:56 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:56 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:56 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:56 DEBUG The Living Valley] Autonomy: Willy->Harvey encounter approved! block=BaseAnchor location=Hospital.
[03:17:56 DEBUG The Living Valley] Autonomy: Willy->Harvey staged successfully, starting conversation.
[03:17:56 DEBUG The Living Valley] Autonomy: Willy->Harvey Player2 encounter conversation launched (turns=4, continuation=False).
[03:17:57 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:57 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:57 TRACE The Living Valley] Autonomy: Haley->Beckett skipped by 50% encounter gate (block=BaseAnchor).
[03:17:57 DEBUG The Living Valley] Autonomy: Beckett->Haley encounter approved! block=ReturnHome location=Town.
[03:17:57 DEBUG The Living Valley] Autonomy: Beckett->Haley staged successfully, starting conversation.
[03:17:57 DEBUG The Living Valley] Autonomy: Beckett->Haley Player2 encounter conversation launched (turns=4, continuation=False).
[03:17:57 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:57 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:57 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:58 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:58 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:58 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:58 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:58 TRACE The Living Valley] Player2 stream line: {"message":"\u003cWilly\u003e Did you see Olivia and the Wizard stirring up that festival buzz?","npc_id":"d08d974a-dea9-41fd-904a-9e711136ddb5"}
[03:17:58 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:58 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:58 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:58 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:58 TRACE The Living Valley] Autonomy: Lewis found target Alex but out of talk range (dist=10.0).
[03:17:58 TRACE The Living Valley] Autonomy: Alex found target Lewis but out of talk range (dist=10.0).
[03:17:58 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=142.0).
[03:17:58 TRACE The Living Valley] Autonomy: Emily found target Marnie but out of talk range (dist=42.0).
[03:17:58 TRACE The Living Valley] Autonomy: Gus found target Alex but out of talk range (dist=87.0).
[03:17:58 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=80.0).
[03:17:58 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=102.0).
[03:17:58 TRACE The Living Valley] Autonomy: Leah found target Marlon but out of talk range (dist=41.0).
[03:17:58 TRACE The Living Valley] Autonomy: Marnie found target Pierre but out of talk range (dist=14.0).
[03:17:58 TRACE The Living Valley] Autonomy: Marlon found target Leah but out of talk range (dist=41.0).
[03:17:58 TRACE The Living Valley] Autonomy: MorrisTod found target Pam but out of talk range (dist=236.0).
[03:17:58 TRACE The Living Valley] Autonomy: Pam found target Shane but out of talk range (dist=7.0).
[03:17:58 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:17:58 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=102.0).
[03:17:58 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:17:58 TRACE The Living Valley] Autonomy: Shane found target Pam but out of talk range (dist=7.0).
[03:17:58 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:17:58 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:17:58 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:17:58 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:17:58 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:17:58 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:17:58 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:17:58 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=80.0).
[03:17:58 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:17:58 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:17:58 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=142.0).
[03:17:58 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=262.0).
[03:17:59 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:59 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:59 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:59 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:59 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:17:59 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:59 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:17:59 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:17:59 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:00 TRACE The Living Valley] Player2 stream line: {"message":"\u003cHarvey\u003e I noted their excitement, though I worry the crowd might strain the clinic later.","npc_id":"88616403-d0f1-4689-8883-1dbcf880c7c5"}
[03:18:00 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:00 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:00 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:00 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:00 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:00 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:00 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:00 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:00 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:01 TRACE The Living Valley] Player2 stream line: {"message":"\u003cHaley\u003e Those lanterns look cool, but it'll be weird if the fair’s lights keep flickering.","npc_id":"399d9c4c-6727-4f2e-821d-6aecd00c6a23"}
[03:18:01 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:01 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:01 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:01 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:01 TRACE The Living Valley] Player2 stream line: {"message":"\u003cWilly\u003e Watch the tide; low water’s coming in later, could affect the docks.","npc_id":"d08d974a-dea9-41fd-904a-9e711136ddb5"}
[03:18:01 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:01 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:01 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:01 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:02 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:02 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:02 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:02 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:02 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:02 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:02 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:02 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:02 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:03 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:03 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:03 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:03 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:03 TRACE The Living Valley] Player2 stream line: {"message":"\u003cHarvey\u003e I will keep an eye on it, thank you for the heads‑up.","npc_id":"88616403-d0f1-4689-8883-1dbcf880c7c5"}
[03:18:03 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:03 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:03 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:03 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:03 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:03 TRACE The Living Valley] Autonomy: Lewis found target Penny but out of talk range (dist=62.0).
[03:18:03 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=10.0).
[03:18:03 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=151.0).
[03:18:03 TRACE The Living Valley] Autonomy: Emily found target Marnie but out of talk range (dist=52.0).
[03:18:03 TRACE The Living Valley] Autonomy: Evelyn found target Alex but out of talk range (dist=144.0).
[03:18:03 TRACE The Living Valley] Autonomy: George found target Alex but out of talk range (dist=10.0).
[03:18:03 TRACE The Living Valley] Autonomy: Gus found target Penny but out of talk range (dist=81.0).
[03:18:03 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=72.0).
[03:18:03 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=111.0).
[03:18:03 TRACE The Living Valley] Autonomy: Leah found target Marlon but out of talk range (dist=41.0).
[03:18:03 TRACE The Living Valley] Autonomy: Marnie found target Pierre but out of talk range (dist=14.0).
[03:18:03 TRACE The Living Valley] Autonomy: Marlon found target Leah but out of talk range (dist=41.0).
[03:18:03 TRACE The Living Valley] Autonomy: MorrisTod found target Pam but out of talk range (dist=246.0).
[03:18:03 TRACE The Living Valley] Autonomy: Pam found target Shane but out of talk range (dist=7.0).
[03:18:03 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:18:03 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=111.0).
[03:18:03 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:03 TRACE The Living Valley] Autonomy: Shane found target Pam but out of talk range (dist=7.0).
[03:18:03 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:18:03 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:18:03 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:18:03 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:18:03 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:18:03 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:18:03 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:18:03 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=72.0).
[03:18:03 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:18:03 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:18:03 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=151.0).
[03:18:03 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=271.0).
[03:18:03 DEBUG The Living Valley] Encounter conversation completed: Willy->Harvey enc=enc_24 turns=4/4 duration_ms=7111.
[03:18:03 TRACE The Living Valley] Encounter transcript T1 Willy->Harvey: Did you see Olivia and the Wizard stirring up that festival buzz?
[03:18:03 TRACE The Living Valley] Encounter transcript T2 Harvey->Willy: I noted their excitement, though I worry the crowd might strain the clinic later.
[03:18:03 TRACE The Living Valley] Encounter transcript T3 Willy->Harvey: Watch the tide; low water’s coming in later, could affect the docks.
[03:18:03 TRACE The Living Valley] Encounter transcript T4 Harvey->Willy: I will keep an eye on it, thank you for the heads‑up.
[03:18:04 TRACE The Living Valley] Player2 stream line: {"message":"\u003cHaley\u003e Anyway, I’ll head back to town and keep an eye on those beams.","npc_id":"399d9c4c-6727-4f2e-821d-6aecd00c6a23"}
[03:18:04 DEBUG The Living Valley] Encounter conversation completed: Beckett->Haley enc=enc_25 turns=4/4 duration_ms=6991.
[03:18:04 TRACE The Living Valley] Encounter transcript T1 Beckett->Haley: Olivia and the Wizard were testing new lanterns at the fair—might affect night visibility.
[03:18:04 TRACE The Living Valley] Encounter transcript T2 Haley->Beckett: Those lanterns look cool, but it'll be weird if the fair’s lights keep flickering.
[03:18:04 TRACE The Living Valley] Encounter transcript T3 Beckett->Haley: If they flicker, I’ll recalibrate the support beams’ monitoring sensors.
[03:18:04 TRACE The Living Valley] Encounter transcript T4 Haley->Beckett: Anyway, I’ll head back to town and keep an eye on those beams.
[03:18:04 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:04 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:04 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:04 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=False).
[03:18:04 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:04 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:04 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:04 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:05 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:05 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:05 TRACE The Living Valley] Autonomy: encounter enc_23 Jodi->Abigail waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:05 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:05 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:05 TRACE The Living Valley] Autonomy: encounter enc_22 Caroline->Robin waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:05 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Jodi->Abigail after complete.
[03:18:05 DEBUG The Living Valley] Autonomy: [HANDOFF] Jodi starting handoff: TilePoint=(15,12), controller=null, followSchedule=True, time=1310, map=SeedShop.
[03:18:05 TRACE The Living Valley] Autonomy: queued Jodi for vanilla schedule resume after encounter enc_23 (complete, restored=False, next_tick=27121, map=SeedShop, time=1310).
[03:18:05 DEBUG The Living Valley] Autonomy: [HANDOFF] Abigail starting handoff: TilePoint=(17,12), controller=null, followSchedule=True, time=1310, map=SeedShop.
[03:18:05 TRACE The Living Valley] Autonomy: queued Abigail for vanilla schedule resume after encounter enc_23 (complete, restored=False, next_tick=27121, map=SeedShop, time=1310).
[03:18:05 DEBUG The Living Valley] Autonomy: Player2 encounter enc_23 Jodi->Abigail completed (outcome=friendly).
[03:18:05 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:05 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Abigail starting rebind at TilePoint=(17,12), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1310.
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Abigail cleared schedule, calling TryLoadSchedule().
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Abigail TryLoadSchedule returned=True, schedule_count=5, first_keys=900,1030,1300,1630,1930.
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Abigail current_time=1310, entries_before_current=900:SeedShop,1030:SeedShop,1300:Town.
[03:18:05 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Abigail encounter=enc_23 from=SeedShop to=Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57) arrival_resolved=True active_target_location=Town active_target_tile=(73,54) time=1310.
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Abigail reset complete: lastAttemptedSchedule=1310, previousEndPoint=(6,30), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1300, next_schedule_time=1630, active_target_location=Town, active_target_tile=(73,54), active_facing=2, active_behavior=none, fallback_used=True.
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Jodi starting rebind at TilePoint=(15,12), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1310.
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Jodi cleared schedule, calling TryLoadSchedule().
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Jodi TryLoadSchedule returned=True, schedule_count=8, first_keys=800,940,1000,1300,1600.
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Jodi current_time=1310, entries_before_current=800:SamHouse,940:SamHouse,1000:SeedShop,1300:SeedShop.
[03:18:05 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Jodi forced same-map active-slot path after encounter enc_23 (active_schedule_time=1300, next_schedule_time=1600, location=SeedShop, tile=(21,17), time=1310).
[03:18:05 DEBUG The Living Valley] Autonomy: [REBIND] Jodi reset complete: lastAttemptedSchedule=1310, previousEndPoint=(21,17), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(21,17), active_facing=2, active_behavior=jodi_exercise, fallback_used=True.
[03:18:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(16,12) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:06 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Caroline->Robin after complete.
[03:18:06 DEBUG The Living Valley] Autonomy: [HANDOFF] Caroline starting handoff: TilePoint=(24,15), controller=null, followSchedule=True, time=1310, map=SeedShop.
[03:18:06 TRACE The Living Valley] Autonomy: queued Caroline for vanilla schedule resume after encounter enc_22 (complete, restored=False, next_tick=27151, map=SeedShop, time=1310).
[03:18:06 DEBUG The Living Valley] Autonomy: [HANDOFF] Robin starting handoff: TilePoint=(24,18), controller=null, followSchedule=True, time=1310, map=SeedShop.
[03:18:06 TRACE The Living Valley] Autonomy: queued Robin for vanilla schedule resume after encounter enc_22 (complete, restored=False, next_tick=27151, map=SeedShop, time=1310).
[03:18:06 DEBUG The Living Valley] Autonomy: Player2 encounter enc_22 Caroline->Robin completed (outcome=friendly).
[03:18:06 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:06 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Robin starting rebind at TilePoint=(24,18), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1310.
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Robin cleared schedule, calling TryLoadSchedule().
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Robin TryLoadSchedule returned=True, schedule_count=5, first_keys=930,1300,1600,1800,2100.
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Robin current_time=1310, entries_before_current=930:SeedShop,1300:SeedShop.
[03:18:06 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Robin forced same-map active-slot path after encounter enc_22 (active_schedule_time=1300, next_schedule_time=1600, location=SeedShop, tile=(27,22), time=1310).
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Robin reset complete: lastAttemptedSchedule=1310, previousEndPoint=(27,22), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(27,22), active_facing=2, active_behavior=robin_exercise, fallback_used=True.
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Caroline starting rebind at TilePoint=(24,15), controller=null, followSchedule=True, temporaryController=null, map=SeedShop, time=1310.
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Caroline cleared schedule, calling TryLoadSchedule().
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Caroline TryLoadSchedule returned=True, schedule_count=6, first_keys=800,1030,1300,1600,1810.
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Caroline current_time=1310, entries_before_current=800:SeedShop,1030:SeedShop,1300:SeedShop.
[03:18:06 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Caroline forced same-map active-slot path after encounter enc_22 (active_schedule_time=1300, next_schedule_time=1600, location=SeedShop, tile=(24,21), time=1310).
[03:18:06 DEBUG The Living Valley] Autonomy: [REBIND] Caroline reset complete: lastAttemptedSchedule=1310, previousEndPoint=(24,21), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(24,21), active_facing=0, active_behavior=caroline_exercise, fallback_used=True.
[03:18:06 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(15,12) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:06 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:06 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:07 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:07 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(14,12) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:07 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(14,13) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:07 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:07 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:08 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(14,14) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:08 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:08 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:08 TRACE SpaceCore] Event: BeforeWarp
[03:18:08 TRACE game] Warping to Farm
[03:18:08 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:08 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:08 TRACE The Living Valley] Autonomy: Lewis found target Penny but out of talk range (dist=62.0).
[03:18:08 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=18.0).
[03:18:08 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=160.0).
[03:18:08 TRACE The Living Valley] Autonomy: Emily found target Marnie but out of talk range (dist=60.0).
[03:18:08 TRACE The Living Valley] Autonomy: Evelyn found target Alex but out of talk range (dist=161.0).
[03:18:08 TRACE The Living Valley] Autonomy: George found target Alex but out of talk range (dist=18.0).
[03:18:08 TRACE The Living Valley] Autonomy: Gus found target Penny but out of talk range (dist=81.0).
[03:18:08 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=72.0).
[03:18:08 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=120.0).
[03:18:08 TRACE The Living Valley] Autonomy: Leah found target Marlon but out of talk range (dist=41.0).
[03:18:08 TRACE The Living Valley] Autonomy: Marnie found target Pierre but out of talk range (dist=14.0).
[03:18:08 TRACE The Living Valley] Autonomy: Marlon found target Leah but out of talk range (dist=41.0).
[03:18:08 TRACE The Living Valley] Autonomy: MorrisTod found target Pam but out of talk range (dist=254.0).
[03:18:08 TRACE The Living Valley] Autonomy: Pam found target Shane but out of talk range (dist=7.0).
[03:18:08 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:18:08 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=120.0).
[03:18:08 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:08 TRACE The Living Valley] Autonomy: Shane found target Pam but out of talk range (dist=7.0).
[03:18:08 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:18:08 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:18:08 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:18:08 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:18:08 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:18:08 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:18:08 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:18:08 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=72.0).
[03:18:08 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:18:08 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:18:08 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=160.0).
[03:18:08 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=280.0).
[03:18:09 TRACE Content Patcher] Requested cache invalidation for all assets matching a predicate.
[03:18:09 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:09 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:09 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:09 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:09 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:09 DEBUG The Living Valley] Autonomy: [ARRIVAL] Caroline active-slot handoff at tile (24,21) in SeedShop (active_schedule_time=1300, active_facing=0, active_behavior=caroline_exercise, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(24,20), facing=2, time=1310).
[03:18:09 DEBUG The Living Valley] Autonomy: returned Caroline to active-slot schedule action after encounter enc_22 (complete, restored=False, attempts=1, active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(24,21), active_facing=0, active_behavior=caroline_exercise, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(24,20), previousEndPoint=(24,21), lastAttemptedSchedule=1310, map=SeedShop, time=1310).
[03:18:09 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(14,15) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:10 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_22 tick=1: controller=PathFindController, isMoving=True, TilePoint=(24,19), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=True.
[03:18:10 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:10 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:10 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_22 tick=2: controller=PathFindController, isMoving=True, TilePoint=(24,19), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=True.
[03:18:10 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_22 tick=3: controller=PathFindController, isMoving=True, TilePoint=(24,19), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=True.
[03:18:10 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(14,16) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:10 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_22 tick=4: controller=PathFindController, isMoving=True, TilePoint=(24,18), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=True.
[03:18:10 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:10 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:10 DEBUG The Living Valley] Autonomy: [ARRIVAL] Robin active-slot handoff at tile (27,22) in SeedShop (active_schedule_time=1300, active_facing=2, active_behavior=robin_exercise, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(27,21), facing=2, time=1310).
[03:18:10 DEBUG The Living Valley] Autonomy: returned Robin to active-slot schedule action after encounter enc_22 (complete, restored=False, attempts=1, active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(27,22), active_facing=2, active_behavior=robin_exercise, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(27,21), previousEndPoint=(27,22), lastAttemptedSchedule=1310, map=SeedShop, time=1310).
[03:18:10 TRACE Farm Type Manager (FTM)] Spawned 1 objects. Time: 1320.
[03:18:10 DEBUG The Living Valley] Autonomy: [MONITOR] Caroline encounter=enc_22 tick=5: controller=PathFindController, isMoving=True, TilePoint=(24,18), moved_from_initial=yes, previousEndPoint=(24,21), followSchedule=True.
[03:18:10 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_22 tick=1: controller=PathFindController, isMoving=True, TilePoint=(27,20), moved_from_initial=yes, previousEndPoint=(27,22), followSchedule=True.
[03:18:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(14,17) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:11 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_22 tick=2: controller=PathFindController, isMoving=True, TilePoint=(27,20), moved_from_initial=yes, previousEndPoint=(27,22), followSchedule=True.
[03:18:11 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:11 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:11 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_22 tick=3: controller=PathFindController, isMoving=True, TilePoint=(27,20), moved_from_initial=yes, previousEndPoint=(27,22), followSchedule=True.
[03:18:11 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_22 tick=4: controller=PathFindController, isMoving=True, TilePoint=(27,19), moved_from_initial=yes, previousEndPoint=(27,22), followSchedule=True.
[03:18:11 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(13,17) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:11 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_22 tick=5: controller=PathFindController, isMoving=True, TilePoint=(27,19), moved_from_initial=yes, previousEndPoint=(27,22), followSchedule=True.
[03:18:11 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:11 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(12,17) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:12 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:12 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:12 DEBUG The Living Valley] Autonomy: Gunther->GuntherSilvian encounter approved! block=ReturnHome location=ArchaeologyHouse.
[03:18:12 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian blocked by wall (no line of sight).
[03:18:12 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(11,17) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:12 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:12 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:12 DEBUG The Living Valley] Autonomy: [ARRIVAL] Jodi active-slot handoff at tile (21,17) in SeedShop (active_schedule_time=1300, active_facing=2, active_behavior=jodi_exercise, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(21,16), facing=2, time=1320).
[03:18:12 DEBUG The Living Valley] Autonomy: returned Jodi to active-slot schedule action after encounter enc_23 (complete, restored=False, attempts=1, active_schedule_time=1300, next_schedule_time=1600, active_target_location=SeedShop, active_target_tile=(21,17), active_facing=2, active_behavior=jodi_exercise, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(21,16), previousEndPoint=(21,17), lastAttemptedSchedule=1320, map=SeedShop, time=1320).
[03:18:12 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_23 tick=1: controller=PathFindController, isMoving=True, TilePoint=(21,16), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=True.
[03:18:13 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_23 tick=2: controller=PathFindController, isMoving=True, TilePoint=(22,16), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=True.
[03:18:13 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:13 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(10,17) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:13 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_23 tick=3: controller=PathFindController, isMoving=True, TilePoint=(22,16), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=True.
[03:18:13 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_23 tick=4: controller=PathFindController, isMoving=True, TilePoint=(22,16), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=True.
[03:18:13 DEBUG The Living Valley] Autonomy: [MONITOR] Jodi encounter=enc_23 tick=5: controller=PathFindController, isMoving=True, TilePoint=(22,16), moved_from_initial=yes, previousEndPoint=(21,17), followSchedule=True.
[03:18:13 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:13 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:13 TRACE The Living Valley] Autonomy: Lewis found target Penny but out of talk range (dist=62.0).
[03:18:13 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=25.0).
[03:18:13 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=167.0).
[03:18:13 TRACE The Living Valley] Autonomy: Emily found target Marnie but out of talk range (dist=68.0).
[03:18:13 TRACE The Living Valley] Autonomy: Evelyn found target Alex but out of talk range (dist=167.0).
[03:18:13 TRACE The Living Valley] Autonomy: George found target Alex but out of talk range (dist=25.0).
[03:18:13 TRACE The Living Valley] Autonomy: Gus found target Penny but out of talk range (dist=81.0).
[03:18:13 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=71.0).
[03:18:13 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=127.0).
[03:18:13 TRACE The Living Valley] Autonomy: Leah found target Marlon but out of talk range (dist=41.0).
[03:18:13 TRACE The Living Valley] Autonomy: Marnie found target Pierre but out of talk range (dist=14.0).
[03:18:13 TRACE The Living Valley] Autonomy: Marlon found target Leah but out of talk range (dist=41.0).
[03:18:13 TRACE The Living Valley] Autonomy: MorrisTod found target Pam but out of talk range (dist=262.0).
[03:18:13 TRACE The Living Valley] Autonomy: Pam found target Shane but out of talk range (dist=7.0).
[03:18:13 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:18:13 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=127.0).
[03:18:13 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:13 TRACE The Living Valley] Autonomy: Shane found target Pam but out of talk range (dist=7.0).
[03:18:13 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=168.0).
[03:18:13 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:18:13 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:18:13 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:18:13 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:18:13 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:18:13 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:18:13 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=71.0).
[03:18:13 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:18:13 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:18:13 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=167.0).
[03:18:13 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=287.0).
[03:18:13 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(9,17) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:14 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:14 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:14 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(9,18) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:14 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:14 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:14 TRACE SpaceCore] Event: BeforeWarp
[03:18:14 TRACE game] Warping to FarmHouse
[03:18:15 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:15 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:15 TRACE Content Patcher] Requested cache invalidation for all assets matching a predicate.
[03:18:15 TRACE SMAPI] Invalidated 2 asset names (LooseSprites/font_bold, Maps/winter_outdoorsTileSheet).
Propagated 2 core assets (LooseSprites/font_bold, Maps/winter_outdoorsTileSheet).
[03:18:15 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:15 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:16 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:16 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(9,19) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:16 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:16 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:16 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(8,19) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:17 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:17 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:17 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Abigail encounter=enc_23 map=SeedShop tile=(7,19) target_leg=SeedShop->Town transition_tile=(6,30) approach_tile=(6,30) arrival_tile=(43,57).
[03:18:17 TRACE The Living Valley] Autonomy: encounter enc_24 Willy->Harvey waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:18:17 TRACE The Living Valley] Autonomy: encounter enc_25 Beckett->Haley waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:17 TRACE SMAPI] Content Patcher edited Strings/Locations (for the 'Stardew Valley Expanded' content pack).
[03:18:17 TRACE The Living Valley] Autonomy: cancelling encounter enc_24 Willy->Harvey (ui_interrupt).
[03:18:17 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Willy->Harvey after ui_interrupt.
[03:18:17 DEBUG The Living Valley] Autonomy: [HANDOFF] Willy starting handoff: TilePoint=(12,14), controller=null, followSchedule=True, time=1320, map=Hospital.
[03:18:17 TRACE The Living Valley] Autonomy: queued Willy for vanilla schedule resume after encounter enc_24 (ui_interrupt, restored=False, next_tick=27845, map=Hospital, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [HANDOFF] Harvey starting handoff: TilePoint=(10,13), controller=null, followSchedule=True, time=1320, map=Hospital.
[03:18:17 TRACE The Living Valley] Autonomy: queued Harvey for vanilla schedule resume after encounter enc_24 (ui_interrupt, restored=False, next_tick=27845, map=Hospital, time=1320).
[03:18:17 TRACE The Living Valley] Autonomy: cancelling encounter enc_25 Beckett->Haley (ui_interrupt).
[03:18:17 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Beckett->Haley after ui_interrupt.
[03:18:17 DEBUG The Living Valley] Autonomy: [HANDOFF] Beckett starting handoff: TilePoint=(21,23), controller=null, followSchedule=True, time=1320, map=Town.
[03:18:17 TRACE The Living Valley] Autonomy: queued Beckett for vanilla schedule resume after encounter enc_25 (ui_interrupt, restored=False, next_tick=27845, map=Town, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [HANDOFF] Haley starting handoff: TilePoint=(24,23), controller=null, followSchedule=True, time=1320, map=Town.
[03:18:17 TRACE The Living Valley] Autonomy: queued Haley for vanilla schedule resume after encounter enc_25 (ui_interrupt, restored=False, next_tick=27845, map=Town, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Haley starting rebind at TilePoint=(24,23), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1320.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Haley cleared schedule, calling TryLoadSchedule().
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Haley TryLoadSchedule returned=True, schedule_count=6, first_keys=900,950,1100,1630,2000.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Haley current_time=1320, entries_before_current=900:HaleyHouse,950:HaleyHouse,1100:Town.
[03:18:17 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Haley already at active-slot destination after encounter enc_25 (active_schedule_time=1100, next_schedule_time=1630, location=Town, tile=(24,23), time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Haley reset complete: lastAttemptedSchedule=1320, previousEndPoint=(24,23), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1100, next_schedule_time=1630, active_target_location=Town, active_target_tile=(24,23), active_facing=2, active_behavior=none, fallback_used=False.
[03:18:17 DEBUG The Living Valley] Autonomy: waiting to return Haley to vanilla schedule after encounter enc_25 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1320, active_schedule_time=1100, next_schedule_time=1630, active_target_location=Town, active_target_tile=(24,23), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(24,23), previousEndPoint=(24,23), lastAttemptedSchedule=1320, map=Town, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Harvey starting rebind at TilePoint=(10,13), controller=null, followSchedule=True, temporaryController=null, map=Hospital, time=1320.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Harvey cleared schedule, calling TryLoadSchedule().
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Harvey TryLoadSchedule returned=True, schedule_count=9, first_keys=730,1250,1330,1410,1550.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Harvey current_time=1320, entries_before_current=730:Hospital,1250:Hospital.
[03:18:17 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Harvey already at active-slot destination after encounter enc_24 (active_schedule_time=1250, next_schedule_time=1330, location=Hospital, tile=(10,14), time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Harvey reset complete: lastAttemptedSchedule=1320, previousEndPoint=(10,13), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1250, next_schedule_time=1330, active_target_location=Hospital, active_target_tile=(10,14), active_facing=2, active_behavior=none, fallback_used=False.
[03:18:17 DEBUG The Living Valley] Autonomy: waiting to return Harvey to vanilla schedule after encounter enc_24 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1320, active_schedule_time=1250, next_schedule_time=1330, active_target_location=Hospital, active_target_tile=(10,14), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(10,13), previousEndPoint=(10,13), lastAttemptedSchedule=1320, map=Hospital, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Beckett starting rebind at TilePoint=(21,23), controller=null, followSchedule=True, temporaryController=null, map=Town, time=1320.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Beckett cleared schedule, calling TryLoadSchedule().
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Beckett TryLoadSchedule returned=True, schedule_count=7, first_keys=620,730,900,1130,1400.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Beckett current_time=1320, entries_before_current=620:DH.Arthur.House,730:Downhill,900:Town,1130:Blacksmith.
[03:18:17 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Beckett encounter=enc_25 from=Town to=Blacksmith transition_tile=(94,81) approach_tile=(94,81) arrival_tile=(5,20) arrival_resolved=True active_target_location=Blacksmith active_target_tile=(7,17) time=1320.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Beckett reset complete: lastAttemptedSchedule=1320, previousEndPoint=(94,81), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1130, next_schedule_time=1400, active_target_location=Blacksmith, active_target_tile=(7,17), active_facing=1, active_behavior=beckett_inspect, fallback_used=True.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Willy starting rebind at TilePoint=(12,14), controller=null, followSchedule=True, temporaryController=null, map=Hospital, time=1320.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Willy cleared schedule, calling TryLoadSchedule().
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Willy TryLoadSchedule returned=True, schedule_count=6, first_keys=610,850,1010,1330,1600.
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Willy current_time=1320, entries_before_current=610:Beach,850:FishShop,1010:Hospital.
[03:18:17 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Willy already at active-slot destination after encounter enc_24 (active_schedule_time=1010, next_schedule_time=1330, location=Hospital, tile=(12,14), time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [REBIND] Willy reset complete: lastAttemptedSchedule=1320, previousEndPoint=(12,14), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=1010, next_schedule_time=1330, active_target_location=Hospital, active_target_tile=(12,14), active_facing=0, active_behavior=none, fallback_used=False.
[03:18:17 DEBUG The Living Valley] Autonomy: waiting to return Willy to vanilla schedule after encounter enc_24 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=1320, active_schedule_time=1010, next_schedule_time=1330, active_target_location=Hospital, active_target_tile=(12,14), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(12,14), previousEndPoint=(12,14), lastAttemptedSchedule=1320, map=Hospital, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [ARRIVAL] Haley active-slot handoff at tile (24,23) in Town (active_schedule_time=1100, active_facing=2, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(24,23), facing=2, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: returned Haley to active-slot schedule action after encounter enc_25 (ui_interrupt, restored=False, attempts=1, active_schedule_time=1100, next_schedule_time=1630, active_target_location=Town, active_target_tile=(24,23), active_facing=2, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(24,23), previousEndPoint=(24,23), lastAttemptedSchedule=1320, map=Town, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [ARRIVAL] Harvey active-slot handoff at tile (10,14) in Hospital (active_schedule_time=1250, active_facing=2, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(10,13), facing=2, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: returned Harvey to active-slot schedule action after encounter enc_24 (ui_interrupt, restored=False, attempts=1, active_schedule_time=1250, next_schedule_time=1330, active_target_location=Hospital, active_target_tile=(10,14), active_facing=2, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(10,13), previousEndPoint=(10,14), lastAttemptedSchedule=1320, map=Hospital, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [ARRIVAL] Willy active-slot handoff at tile (12,14) in Hospital (active_schedule_time=1010, active_facing=0, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(12,14), facing=0, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: returned Willy to active-slot schedule action after encounter enc_24 (ui_interrupt, restored=False, attempts=1, active_schedule_time=1010, next_schedule_time=1330, active_target_location=Hospital, active_target_tile=(12,14), active_facing=0, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(12,14), previousEndPoint=(12,14), lastAttemptedSchedule=1320, map=Hospital, time=1320).
[03:18:17 DEBUG The Living Valley] Autonomy: [MONITOR] Willy encounter=enc_24 tick=1: controller=PathFindController, isMoving=False, TilePoint=(12,14), moved_from_initial=no, previousEndPoint=(12,14), followSchedule=True.
[03:18:17 DEBUG The Living Valley] Autonomy: [MONITOR] Haley encounter=enc_25 tick=1: controller=PathFindController, isMoving=False, TilePoint=(24,23), moved_from_initial=no, previousEndPoint=(24,23), followSchedule=True.
[03:18:17 DEBUG The Living Valley] Autonomy: [MONITOR] Harvey encounter=enc_24 tick=1: controller=PathFindController, isMoving=False, TilePoint=(10,13), moved_from_initial=no, previousEndPoint=(10,14), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Willy encounter=enc_24 tick=2: controller=PathFindController, isMoving=False, TilePoint=(12,14), moved_from_initial=no, previousEndPoint=(12,14), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Haley encounter=enc_25 tick=2: controller=PathFindController, isMoving=False, TilePoint=(24,23), moved_from_initial=no, previousEndPoint=(24,23), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Harvey encounter=enc_24 tick=2: controller=PathFindController, isMoving=False, TilePoint=(10,13), moved_from_initial=no, previousEndPoint=(10,14), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Willy encounter=enc_24 tick=3: controller=PathFindController, isMoving=False, TilePoint=(12,14), moved_from_initial=no, previousEndPoint=(12,14), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Haley encounter=enc_25 tick=3: controller=PathFindController, isMoving=False, TilePoint=(24,23), moved_from_initial=no, previousEndPoint=(24,23), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Harvey encounter=enc_24 tick=3: controller=PathFindController, isMoving=False, TilePoint=(10,13), moved_from_initial=no, previousEndPoint=(10,14), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Willy encounter=enc_24 tick=4: controller=PathFindController, isMoving=False, TilePoint=(12,14), moved_from_initial=no, previousEndPoint=(12,14), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Haley encounter=enc_25 tick=4: controller=PathFindController, isMoving=False, TilePoint=(24,23), moved_from_initial=no, previousEndPoint=(24,23), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Harvey encounter=enc_24 tick=4: controller=PathFindController, isMoving=False, TilePoint=(10,13), moved_from_initial=no, previousEndPoint=(10,14), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Willy encounter=enc_24 tick=5: controller=PathFindController, isMoving=False, TilePoint=(12,14), moved_from_initial=no, previousEndPoint=(12,14), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Haley encounter=enc_25 tick=5: controller=PathFindController, isMoving=False, TilePoint=(24,23), moved_from_initial=no, previousEndPoint=(24,23), followSchedule=True.
[03:18:18 DEBUG The Living Valley] Autonomy: [MONITOR] Harvey encounter=enc_24 tick=5: controller=PathFindController, isMoving=False, TilePoint=(10,13), moved_from_initial=no, previousEndPoint=(10,14), followSchedule=True.
[03:18:18 TRACE The Living Valley] Autonomy: Lewis found target Haley but out of talk range (dist=64.0).
[03:18:18 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=25.0).
[03:18:18 TRACE The Living Valley] Autonomy: Elliott found target Daulton but out of talk range (dist=173.0).
[03:18:18 TRACE The Living Valley] Autonomy: Emily found target Marnie but out of talk range (dist=73.0).
[03:18:18 TRACE The Living Valley] Autonomy: Evelyn found target Alex but out of talk range (dist=172.0).
[03:18:18 TRACE The Living Valley] Autonomy: George found target Alex but out of talk range (dist=25.0).
[03:18:18 TRACE The Living Valley] Autonomy: Gus found target Penny but out of talk range (dist=81.0).
[03:18:18 TRACE The Living Valley] Autonomy: Jas found target Andy but out of talk range (dist=74.0).
[03:18:18 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=132.0).
[03:18:18 TRACE The Living Valley] Autonomy: Leah found target Marlon but out of talk range (dist=41.0).
[03:18:18 TRACE The Living Valley] Autonomy: Marnie found target Abigail but out of talk range (dist=11.0).
[03:18:18 TRACE The Living Valley] Autonomy: Marlon found target Beckett but out of talk range (dist=19.0).
[03:18:18 TRACE The Living Valley] Autonomy: Maru found target Harvey but out of talk range (dist=6.0).
[03:18:18 TRACE The Living Valley] Autonomy: MorrisTod found target Pam but out of talk range (dist=267.0).
[03:18:18 TRACE The Living Valley] Autonomy: Pam found target Shane but out of talk range (dist=7.0).
[03:18:18 TRACE The Living Valley] Autonomy: Penny found target Lewis but out of talk range (dist=62.0).
[03:18:18 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=132.0).
[03:18:18 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:18 TRACE The Living Valley] Autonomy: Shane found target Pam but out of talk range (dist=7.0).
[03:18:18 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=169.0).
[03:18:18 TRACE The Living Valley] Autonomy: Morrow found target Martin but out of talk range (dist=8.0).
[03:18:18 TRACE The Living Valley] Autonomy: Chloe found target Anderson but out of talk range (dist=10.0).
[03:18:18 TRACE The Living Valley] Autonomy: Anderson found target Chloe but out of talk range (dist=10.0).
[03:18:18 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=12.0).
[03:18:18 TRACE The Living Valley] Autonomy: Julia found target Chloe but out of talk range (dist=102.0).
[03:18:18 TRACE The Living Valley] Autonomy: Alesia found target Sludge but out of talk range (dist=18.0).
[03:18:18 TRACE The Living Valley] Autonomy: Andy found target Jas but out of talk range (dist=74.0).
[03:18:18 TRACE The Living Valley] Autonomy: Jolyne found target Gale but out of talk range (dist=14.0).
[03:18:18 TRACE The Living Valley] Autonomy: Martin found target Morrow but out of talk range (dist=8.0).
[03:18:18 TRACE The Living Valley] Autonomy: Daulton found target Elliott but out of talk range (dist=173.0).
[03:18:18 TRACE The Living Valley] Autonomy: MarchFoM found target Clint but out of talk range (dist=292.0).
[03:18:20 TRACE Farm Type Manager (FTM)] Day is ending. Processing save data and object expiration settings.
[03:18:20 TRACE SMAPI] Synchronizing 'NewDay' task...
[03:18:20 TRACE SMAPI] Content Patcher loaded asset 'Characters/schedules/Morrow' (for the 'Living Valley Characters - CP' content pack).
[03:18:20 TRACE SMAPI] Content Patcher edited Characters/schedules/Morrow (for the 'Living Valley Characters - CP' content pack).
[03:18:20 TRACE SMAPI] Content Patcher loaded asset 'Portraits/Chloe' (for the 'Living Valley Downhill - CP' content pack).
[03:18:20 TRACE SMAPI] Content Patcher loaded asset 'Characters/Chloe' (for the 'Living Valley Downhill - CP' content pack).
[03:18:20 TRACE SpaceCore] Event: ChooseNightlyFarmEvent
[03:18:20 TRACE SMAPI] Content Patcher edited Strings/Events (for the 'Stardew Valley Expanded' content pack).
[03:18:21 TRACE SMAPI]    task complete.
[03:18:21 TRACE The Living Valley] Ambient command unlocks day 122: adjust_town_sentiment | events=6 public=3 market=0 scarcity=0 oversupply=0 anomaly=False
[03:18:21 DEBUG The Living Valley] Autonomy: [REBIND] Martin starting rebind at TilePoint=(1,3), controller=null, followSchedule=True, temporaryController=null, map=Custom_Martin_WarpRoom, time=600.
[03:18:21 DEBUG The Living Valley] Autonomy: [REBIND] Martin cleared schedule, calling TryLoadSchedule().
[03:18:21 DEBUG The Living Valley] Autonomy: [REBIND] Martin TryLoadSchedule returned=True, schedule_count=0, first_keys=none.
[03:18:21 WARN  The Living Valley] Autonomy: [REBIND] Martin aborting rebind because no schedule loaded.
[03:18:21 DEBUG The Living Valley] Autonomy: vanilla schedule for Martin has no future slot after encounter enc_17 (ui_interrupt, restored=False, attempts=2, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=600, active_schedule_time=800, next_schedule_time=none, active_target_location=JojaMart, active_target_tile=(9,25), fallback_used=False, resumed=false, controller=null, isMoving=False, temporary_controller=False, TilePoint=(1,3), previousEndPoint=(1,3), lastAttemptedSchedule=-1, map=Custom_Martin_WarpRoom, time=600).
[03:18:21 DEBUG The Living Valley] Applied NPC command lane=auto: adjust_reputation -> outcome olivia (intent=auto_rep_evt_122_social_121_72004_olivia)
[03:18:21 DEBUG The Living Valley] Applied NPC command lane=auto: shift_interest_influence -> outcome farmers_circle (intent=auto_interest_122_farmers_circle_6)
[03:18:21 DEBUG The Living Valley] Applied NPC command lane=auto: propose_quest -> outcome quest_ai_social_visit_olivia_122_2833 (intent=auto_evt_q_122_social_121_72004_social_visit_Olivia)
[03:18:21 TRACE The Living Valley] Quest mapping | requested: template=social_visit, target=Olivia, urgency=high | applied: template=social_visit, target=olivia, urgency=high, count=1, reward=400, expires+2d | fallback=False
[03:18:23 TRACE The Living Valley] Player2 stream line: {"message":"\u003cRobin\u003e Anyway, let’s get those posts bolted before the sun sets tonight.","npc_id":"973709cb-a3de-4750-a18a-875eff02e2a4"}
[03:18:24 TRACE The Living Valley] Autonomy: Robin found target Maru but out of talk range (dist=19.0).
[03:18:24 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:18:24 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=30.0).
[03:18:24 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:18:24 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:18:24 TRACE The Living Valley] Autonomy: Evelyn found target Alex but out of talk range (dist=29.0).
[03:18:24 TRACE The Living Valley] Autonomy: George found target Alex but out of talk range (dist=20.0).
[03:18:24 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:18:24 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:18:24 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:18:24 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:18:24 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:18:24 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:18:24 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:18:24 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:18:24 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:24 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:18:24 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:18:24 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=19.0).
[03:18:24 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=10.0).
[03:18:24 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=10.0).
[03:18:24 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:18:24 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:29 TRACE The Living Valley] Autonomy: Robin found target Maru but out of talk range (dist=19.0).
[03:18:29 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:18:29 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:18:29 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=30.0).
[03:18:29 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:18:29 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:18:29 TRACE The Living Valley] Autonomy: Evelyn found target Alex but out of talk range (dist=29.0).
[03:18:29 TRACE The Living Valley] Autonomy: George found target Alex but out of talk range (dist=20.0).
[03:18:29 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:18:29 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:18:29 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:18:29 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:18:29 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:18:29 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:18:29 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:18:29 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:18:29 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:18:29 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:29 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:18:29 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:18:29 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=19.0).
[03:18:29 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=10.0).
[03:18:29 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=10.0).
[03:18:29 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:18:29 TRACE SpaceCore] Event: BeforeWarp
[03:18:29 TRACE game] Warping to FarmHouse
[03:18:29 TRACE SpaceCore] Event: ShowNightEndMenus
[03:18:29 DEBUG SpaceCore] Doing skill menus
[03:18:29 TRACE SMAPI] Context: before save.
[03:18:29 TRACE SpaceCore] Saving custom data
[03:18:29 TRACE game] SaveGame.Save() called.
[03:18:29 TRACE SMAPI] Synchronizing 'Save' task...
[03:18:29 TRACE game] Saving without compression...
[03:18:31 TRACE SMAPI]    task complete.
[03:18:31 TRACE game] SaveGame.Save() completed without exceptions.
[03:18:32 TRACE game] Applied trigger action 'SVE_ChallengingBadlands' with actions [RemoveMail Current SVE_ChallengingBadlands received].
[03:18:32 TRACE game] Applied trigger action 'SVE_ChallengingMarshlands' with actions [RemoveMail Current SVE_ChallengingMarshlands received].
[03:18:32 TRACE game] Applied trigger action 'SVE_ChallengingHighlandsCavern' with actions [RemoveMail Current SVE_ChallengingHighlandsCavern received].
[03:18:32 TRACE game] Applied trigger action 'SVE_LowMemoryBadlands' with actions [RemoveMail Current SVE_LowMemoryBadlands received].
[03:18:32 TRACE game] Applied trigger action 'SVE_LowMemoryHighlands' with actions [RemoveMail Current SVE_LowMemoryHighlands received].
[03:18:32 TRACE game] Applied trigger action 'FlashShifter.StardewValleyExpandedCP_LanceRiceRemove' with actions [RemoveMail Current SVE.LanceRice].
[03:18:32 TRACE SMAPI] Context: after save, starting spring 10 Y2.
[03:18:32 TRACE The Living Valley] Player family awareness propagation (day-start): no new spreads.
[03:18:32 TRACE The Living Valley] Player family unchanged (day-start): spouse=none children=0.
[03:18:32 TRACE The Living Valley] Ambient command unlocks day 122: adjust_town_sentiment | events=6 public=3 market=0 scarcity=0 oversupply=0 anomaly=False
[03:18:32 DEBUG The Living Valley] Autonomy: built 63 daily plans.
[03:18:32 TRACE The Living Valley] DailyTickService.Run executed (scaffold).
[03:18:32 TRACE The Living Valley] BuildIssue (sync): Player2 client is NOT null, _player2Key is provided
[03:18:32 DEBUG The Living Valley] Daily tick complete for day 122 (spring Y2).
[03:18:32 TRACE The Living Valley] BuildIssue (sync): ApiBaseUrl='https://api.player2.game/v1'
[03:18:32 TRACE The Living Valley] BuildIssue (sync): SetCredentials called with baseUrl=https://api.player2.game/v1
[03:18:32 TRACE The Living Valley] BuildIssue (sync): Stored player2 to _player2 field, _player2 is now NOT null
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/TractorMod/assets/tractor'.
[03:18:32 TRACE The Living Valley] BuildIssueAsync: Starting, _player2 is NOT null
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/Pathoschild.TractorMod/assets/tractor'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/TractorMod/tractor'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/Pathoschild.TractorMod/tractor'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/TractorMod/Tractor'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/Pathoschild.TractorMod/Tractor'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/TractorMod/assets/tractor.png'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/Pathoschild.TractorMod/assets/tractor.png'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/TractorMod/BuffIcon'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/Pathoschild.TractorMod/BuffIcon'.
[03:18:32 INFO  Tractor Skins] Applying buff icon 'M08' from 'assets/Buff08.png' for 'Mods/Pathoschild.TractorMod/BuffIcon'.
[03:18:32 TRACE SMAPI] Tractor Mod loaded asset 'Mods/Pathoschild.TractorMod/BuffIcon'.
[03:18:32 TRACE SMAPI] Tractor Skins edited Mods/Pathoschild.TractorMod/BuffIcon.
[03:18:32 TRACE SMAPI] Invalidated 1 asset names (Mods/Pathoschild.TractorMod/BuffIcon).
Propagated 1 core assets (Mods/Pathoschild.TractorMod/BuffIcon).
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/TractorMod/bufficon'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/Pathoschild.TractorMod/bufficon'.
[03:18:32 INFO  Tractor Skins] Applying buff icon 'M08' from 'assets/Buff08.png' for 'Mods/Pathoschild.TractorMod/BuffIcon'.
[03:18:32 TRACE SMAPI] Tractor Mod loaded asset 'Mods/Pathoschild.TractorMod/BuffIcon'.
[03:18:32 TRACE SMAPI] Tractor Skins edited Mods/Pathoschild.TractorMod/BuffIcon.
[03:18:32 TRACE SMAPI] Invalidated 1 asset names (Mods/Pathoschild.TractorMod/BuffIcon).
Propagated 1 core assets (Mods/Pathoschild.TractorMod/BuffIcon).
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/TractorMod/assets/bufficon'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/Pathoschild.TractorMod/assets/bufficon'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/TractorMod/assets/bufficon.png'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Requested cache invalidation for 'Mods/Pathoschild.TractorMod/assets/bufficon.png'.
[03:18:32 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:32 TRACE Tractor Skins] Invalidated tractor texture and buff icon cache aliases (day started).
[03:18:33 TRACE Content Patcher] Requested cache invalidation for all assets matching a predicate.
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Alex (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Haley (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Sam (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Lewis (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Lewis (for the 'Downhill Project NPCs' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Caroline (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Pierre (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Penny (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Elliott (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Marnie (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Leah (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Leah (for the 'Downhill Project NPCs' content pack).
[03:18:33 TRACE SMAPI] Content Patcher loaded asset 'Characters/schedules/Susan' (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Characters/schedules/Susan (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Custom_GrampletonSuburbs' (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Maps/Custom_GrampletonSuburbs (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Forest' (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Maps/Forest (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Railroad' (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Maps/Railroad (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Custom_JojaEmporium' (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Maps/Custom_JojaEmporium (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Custom_AdventurerSummit' (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Maps/Custom_AdventurerSummit (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Custom_ForestWest' (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Maps/Custom_ForestWest (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Mountain' (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Maps/Mountain (for the 'Downhill Project' content pack).
[03:18:33 TRACE SMAPI] Content Patcher loaded asset 'Maps/Town' (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Content Patcher edited Maps/Town (for the 'Stardew Valley Expanded' content pack).
[03:18:33 TRACE SMAPI] Invalidated 23 asset names (Characters/Dialogue/Claire, Characters/Dialogue/Daulton, Characters/Dialogue/Maple, Characters/schedules/Alex, Characters/schedules/Caroline, Characters/schedules/Elliott, Characters/schedules/Haley, Characters/schedules/Leah, Characters/schedules/Lewis, Characters/schedules/Marnie, Characters/schedules/Penny, Characters/schedules/Pierre, Characters/schedules/Sam, Characters/schedules/Susan, Data/MoviesReactions, Maps/Custom_AdventurerSummit, Maps/Custom_ForestWest, Maps/Custom_GrampletonSuburbs, Maps/Custom_JojaEmporium, Maps/Forest, Maps/Mountain, Maps/Railroad, Maps/Town).
Propagated 23 core assets (Characters/Dialogue/Claire, Characters/Dialogue/Daulton, Characters/Dialogue/Maple, Characters/schedules/Alex, Characters/schedules/Caroline, Characters/schedules/Elliott, Characters/schedules/Haley, Characters/schedules/Leah, Characters/schedules/Lewis, Characters/schedules/Marnie, Characters/schedules/Penny, Characters/schedules/Pierre, Characters/schedules/Sam, Characters/schedules/Susan, Data/MoviesReactions, Maps/Custom_AdventurerSummit, Maps/Custom_ForestWest, Maps/Custom_GrampletonSuburbs, Maps/Custom_JojaEmporium, Maps/Forest, Maps/Mountain, Maps/Railroad, Maps/Town).
[03:18:33 TRACE Farm Type Manager (FTM)] Loading content packs and local data files.
[03:18:33 TRACE Farm Type Manager (FTM)] Checking for saved objects that need to be respawned overnight or after loading.
[03:18:33 TRACE Farm Type Manager (FTM)] Missing objects: 1. Respawned: 1. Not respawned due to obstructions: 0. Skipped due to missing maps: 0. Skipped due to missing item types: 0.
[03:18:33 TRACE Farm Type Manager (FTM)] Generating forage for content pack: Stardew Valley Expanded Farm Type Manager
[03:18:33 TRACE Farm Type Manager (FTM)] Generating forage for content pack: Downhill Project Extras
[03:18:33 TRACE Farm Type Manager (FTM)] Generating large objects for content pack: Stardew Valley Expanded Farm Type Manager
[03:18:33 TRACE Farm Type Manager (FTM)] Generating ore for content pack: Stardew Valley Expanded Farm Type Manager
[03:18:33 TRACE Farm Type Manager (FTM)] Generating ore for content pack: Downhill Project Extras
[03:18:33 TRACE Farm Type Manager (FTM)] Generating monsters for content pack: Stardew Valley Expanded Farm Type Manager
[03:18:34 TRACE Farm Type Manager (FTM)] Spawned 2095 objects. Time: 600.
[03:18:34 DEBUG The Living Valley] Autonomy: Demetrius->Robin encounter approved! block=BaseAnchor location=ScienceHouse.
[03:18:34 DEBUG The Living Valley] Autonomy: Demetrius->Robin staged successfully, starting conversation.
[03:18:34 DEBUG The Living Valley] Autonomy: Demetrius->Robin Player2 encounter conversation launched (turns=4, continuation=False).
[03:18:34 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian skipped by 50% encounter gate (block=Wander).
[03:18:34 TRACE The Living Valley] Autonomy: GuntherSilvian->Gunther skipped by 50% encounter gate (block=BaseAnchor).
[03:18:34 TRACE The Living Valley] Player2 stream line: {"message":"\u003cEditor\u003e {\"title\":\"Festival Buzz Spreads Fast\",\"content\":\"Tue. 9 (Spring Year 2). Olivia and Wizard were seen joining festival activity near Town. Residents say the news spread quickly through town.\"}","npc_id":"25267022-307c-4ac7-af99-4bfc4e092fce"}
[03:18:34 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian skipped by 50% encounter gate (block=Wander).
[03:18:34 TRACE The Living Valley] Autonomy: GuntherSilvian->Gunther skipped by 50% encounter gate (block=BaseAnchor).
[03:18:35 DEBUG The Living Valley] Event rewrite applied via Player2 (kind=social, title='Festival Buzz Spreads Fast').
[03:18:35 TRACE The Living Valley] SelectHeadlineAsync: Reporter-derived ambient article found: Festival Buzz Spreads Fast
[03:18:35 TRACE The Living Valley] SelectHeadlineAsync: Calling Player2.GenerateSensationalHeadlineAsync for: Festival Buzz Spreads Fast
[03:18:35 TRACE The Living Valley] Player2 stream line: {"message":"\u003cPlanner\u003e {\"target_zone\":\"square\",\"target_spot_role\":\"visitor_idle\",\"target_tile_x\":30,\"target_tile_y\":55,\"leave_map_if_crowded\":false,\"reason\":\"Marlon wandering\"}","npc_id":"806d597f-f1b0-4051-a4c6-c8639cea90ee"}
[03:18:35 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:35 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian skipped by 50% encounter gate (block=Wander).
[03:18:35 TRACE The Living Valley] Autonomy: GuntherSilvian->Gunther skipped by 50% encounter gate (block=BaseAnchor).
[03:18:35 TRACE The Living Valley] Player2 stream line: {"message":"\u003cPlanner\u003e {\"target_zone\":\"square\",\"target_spot_role\":\"visitor_idle\",\"target_tile_x\":30,\"target_tile_y\":55,\"leave_map_if_crowded\":false,\"reason\":\"Gunther wandering\"}","npc_id":"806d597f-f1b0-4051-a4c6-c8639cea90ee"}
[03:18:35 TRACE The Living Valley] Player2 stream line: {"message":"\u003cPlanner\u003e {\"target_zone\":\"square\",\"target_spot_role\":\"visitor_idle\",\"target_tile_x\":30,\"target_tile_y\":55,\"leave_map_if_crowded\":false,\"reason\":\"Krobus wandering\"}","npc_id":"806d597f-f1b0-4051-a4c6-c8639cea90ee"}
[03:18:35 TRACE The Living Valley] Player2 stream line: {"message":"\u003cPlanner\u003e {\"target_zone\":\"house\",\"target_spot_role\":\"home_idle\",\"target_tile_x\":12,\"target_tile_y\":8,\"leave_map_if_crowded\":false,\"reason\":\"Emily home idle\"}","npc_id":"806d597f-f1b0-4051-a4c6-c8639cea90ee"}
[03:18:35 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:35 TRACE The Living Valley] Player2 stream line: {"message":"\u003cDemetrius\u003e It is curious that Olivia and the Wizard were still fiddling with the festival fireworks this morning.","npc_id":"43365615-6939-4a08-86d3-31d4eb07f7cf"}
[03:18:35 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:35 DEBUG The Living Valley] Autonomy: Gunther->GuntherSilvian encounter approved! block=Wander location=ArchaeologyHouse.
[03:18:35 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian blocked by wall (no line of sight).
[03:18:36 TRACE The Living Valley] Player2 stream line: {"message":"\u003cEditor\u003e Wizard and Olivia Ignite Festival Frenzy!","npc_id":"25267022-307c-4ac7-af99-4bfc4e092fce"}
[03:18:36 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:36 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:18:36 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:18:36 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:18:36 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:18:36 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:18:36 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:18:36 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:18:36 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:18:36 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:18:36 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:18:36 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:18:36 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:18:36 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:18:36 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:18:36 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:18:36 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:18:36 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:36 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:18:36 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:18:36 TRACE The Living Valley] Autonomy: Beckett found target Arthur but out of talk range (dist=10.0).
[03:18:36 TRACE The Living Valley] Autonomy: Arthur found target Beckett but out of talk range (dist=10.0).
[03:18:36 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:18:36 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=19.0).
[03:18:36 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:18:36 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:18:36 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:18:36 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=10.0).
[03:18:36 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=10.0).
[03:18:36 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:18:36 TRACE SpaceCore] Event: BeforeWarp
[03:18:36 TRACE game] Warping to Farm
[03:18:36 DEBUG The Living Valley] SelectHeadlineAsync: Got headline: Wizard and Olivia Ignite Festival Frenzy!
[03:18:36 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:36 TRACE The Living Valley] Player2 stream line: {"message":"\u003cRobin\u003e I’ll run a safety review; those sparks can’t be left unchecked.","npc_id":"973709cb-a3de-4750-a18a-875eff02e2a4"}
[03:18:37 TRACE Content Patcher] Requested cache invalidation for all assets matching a predicate.
[03:18:37 TRACE SMAPI] Content Patcher edited LooseSprites/font_bold (for the 'Stardew Valley Expanded' content pack).
[03:18:37 TRACE SMAPI] Content Patcher edited Maps/winter_outdoorsTileSheet (for the 'Stardew Valley Expanded' content pack).
[03:18:37 TRACE SMAPI] Invalidated 2 asset names (LooseSprites/font_bold, Maps/winter_outdoorsTileSheet).
Propagated 2 core assets (LooseSprites/font_bold, Maps/winter_outdoorsTileSheet).
[03:18:37 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:37 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:37 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:37 TRACE The Living Valley] Player2 stream line: {"message":"\u003cEditor\u003e {\"outlook\":[\"Rhubarb edges up slightly—still a solid sell tomorrow.\",\"Parsnip scarcity stays high; consider holding for better prices.\"]}","npc_id":"25267022-307c-4ac7-af99-4bfc4e092fce"}
[03:18:37 DEBUG The Living Valley] BuildIssue (sync): Complete, headline='Wizard and Olivia Ignite Festival Frenzy!'
[03:18:37 DEBUG The Living Valley] Newspaper build completed: day=122, headline='Wizard and Olivia Ignite Festival Frenzy!'
[03:18:37 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:37 DEBUG The Living Valley] Recycled Player2 stream after day-start newspaper build (day 122).
[03:18:38 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:38 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:39 TRACE The Living Valley] Player2 stream line: {"message":"\u003cStagehand\u003e Give it a try, guess the word.","npc_id":"5207e740-a2ce-44d3-8c94-22c95488f846"}
[03:18:39 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:39 TRACE The Living Valley] Player2 stream line: {"message":"\u003cDemetrius\u003e Ensure the dynamite storage is sealed; any misfire could jeopardize the harvest.","npc_id":"43365615-6939-4a08-86d3-31d4eb07f7cf"}
[03:18:39 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:40 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:40 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:40 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:40 TRACE The Living Valley] Player2 stream line: {"message":"\u003cRobin\u003e Well, back to the workshop—got a lot to prep before sunrise.","npc_id":"973709cb-a3de-4750-a18a-875eff02e2a4"}
[03:18:41 TRACE SpaceCore] Event: BeforeWarp
[03:18:41 TRACE game] Warping to BusStop
[03:18:41 DEBUG The Living Valley] Encounter conversation completed: Demetrius->Robin enc=enc_26 turns=4/4 duration_ms=6876.
[03:18:41 TRACE The Living Valley] Encounter transcript T1 Demetrius->Robin: It is curious that Olivia and the Wizard were still fiddling with the festival fireworks this morning.
[03:18:41 TRACE The Living Valley] Encounter transcript T2 Robin->Demetrius: I’ll run a safety review; those sparks can’t be left unchecked.
[03:18:41 TRACE The Living Valley] Encounter transcript T3 Demetrius->Robin: Ensure the dynamite storage is sealed; any misfire could jeopardize the harvest.
[03:18:41 TRACE The Living Valley] Encounter transcript T4 Robin->Demetrius: Well, back to the workshop—got a lot to prep before sunrise.
[03:18:41 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:41 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:18:41 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:18:41 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:18:41 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:18:41 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:18:41 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:18:41 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:18:41 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:18:41 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:18:41 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:18:41 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:18:41 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:18:41 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:18:41 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:18:41 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:18:41 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:18:41 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:41 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:18:41 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:18:41 TRACE The Living Valley] Autonomy: Beckett found target Arthur but out of talk range (dist=10.0).
[03:18:41 TRACE The Living Valley] Autonomy: Arthur found target Beckett but out of talk range (dist=10.0).
[03:18:41 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:18:41 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=19.0).
[03:18:41 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:18:41 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:18:41 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:18:41 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=10.0).
[03:18:41 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=10.0).
[03:18:41 TRACE The Living Valley] Autonomy: MarlonFay found target Marlon but out of talk range (dist=113.0).
[03:18:41 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:41 TRACE Content Patcher] Requested cache invalidation for all assets matching a predicate.
[03:18:41 TRACE SMAPI] Invalidated 0 cache entries.
[03:18:41 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:42 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:42 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:43 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:43 TRACE SMAPI] Content Patcher loaded asset 'Characters/Dialogue/Maple' (for the 'Daulton - Marriageable Character' content pack).
[03:18:43 TRACE SMAPI] Content Patcher edited Characters/Dialogue/Maple (for the 'Daulton - Marriageable Character' content pack).
[03:18:43 TRACE SMAPI] Content Patcher loaded asset 'Characters/Dialogue/Daulton' (for the 'Daulton - Marriageable Character' content pack).
[03:18:43 TRACE SMAPI] Content Patcher edited Characters/Dialogue/Daulton (for the 'Daulton - Marriageable Character' content pack).
[03:18:43 TRACE The Living Valley] Autonomy: encounter enc_26 Demetrius->Robin waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:44 TRACE The Living Valley] Autonomy: cancelling encounter enc_26 Demetrius->Robin (ui_interrupt).
[03:18:44 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Demetrius->Robin after ui_interrupt.
[03:18:44 DEBUG The Living Valley] Autonomy: [HANDOFF] Demetrius starting handoff: TilePoint=(19,4), controller=null, followSchedule=True, time=610, map=ScienceHouse.
[03:18:44 TRACE The Living Valley] Autonomy: queued Demetrius for vanilla schedule resume after encounter enc_26 (ui_interrupt, restored=False, next_tick=29277, map=ScienceHouse, time=610).
[03:18:44 DEBUG The Living Valley] Autonomy: [HANDOFF] Robin starting handoff: TilePoint=(21,4), controller=null, followSchedule=True, time=610, map=ScienceHouse.
[03:18:44 TRACE The Living Valley] Autonomy: queued Robin for vanilla schedule resume after encounter enc_26 (ui_interrupt, restored=False, next_tick=29277, map=ScienceHouse, time=610).
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Demetrius starting rebind at TilePoint=(19,4), controller=null, followSchedule=True, temporaryController=null, map=ScienceHouse, time=610.
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Demetrius cleared schedule, calling TryLoadSchedule().
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Demetrius TryLoadSchedule returned=True, schedule_count=8, first_keys=750,1100,1430,1530,1900.
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Demetrius current_time=610, entries_before_current=.
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Demetrius reset complete: lastAttemptedSchedule=610, previousEndPoint=(19,4), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=none, next_schedule_time=750, active_target_location=none, active_target_tile=none, active_facing=none, active_behavior=none, fallback_used=False.
[03:18:44 DEBUG The Living Valley] Autonomy: waiting to return Demetrius to vanilla schedule after encounter enc_26 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=610, active_schedule_time=none, next_schedule_time=750, active_target_location=none, active_target_tile=none, fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(19,4), previousEndPoint=(19,4), lastAttemptedSchedule=610, map=ScienceHouse, time=610).
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Robin starting rebind at TilePoint=(21,4), controller=null, followSchedule=True, temporaryController=null, map=ScienceHouse, time=610.
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Robin cleared schedule, calling TryLoadSchedule().
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Robin TryLoadSchedule returned=True, schedule_count=4, first_keys=800,1700,1930,2100.
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Robin current_time=610, entries_before_current=.
[03:18:44 DEBUG The Living Valley] Autonomy: [REBIND] Robin reset complete: lastAttemptedSchedule=610, previousEndPoint=(21,4), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=none, next_schedule_time=800, active_target_location=none, active_target_tile=none, active_facing=none, active_behavior=none, fallback_used=False.
[03:18:44 DEBUG The Living Valley] Autonomy: waiting to return Robin to vanilla schedule after encounter enc_26 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=610, active_schedule_time=none, next_schedule_time=800, active_target_location=none, active_target_tile=none, fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(21,4), previousEndPoint=(21,4), lastAttemptedSchedule=610, map=ScienceHouse, time=610).
[03:18:46 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:18:46 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:18:46 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:18:46 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:18:46 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:18:46 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:18:46 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:18:46 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:18:46 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:18:46 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:18:46 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:18:46 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:18:46 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:18:46 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:18:46 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:18:46 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:18:46 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:18:46 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:46 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:18:46 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:18:46 TRACE The Living Valley] Autonomy: Beckett found target Arthur but out of talk range (dist=9.0).
[03:18:46 TRACE The Living Valley] Autonomy: Arthur found target Beckett but out of talk range (dist=9.0).
[03:18:46 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:18:46 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=18.0).
[03:18:46 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:18:46 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:18:46 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:18:46 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=10.0).
[03:18:46 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=10.0).
[03:18:46 TRACE The Living Valley] Autonomy: MarlonFay found target Brock but out of talk range (dist=10.0).
[03:18:49 DEBUG The Living Valley] Autonomy: Beckett->Arthur encounter approved! block=BaseAnchor location=DH.Arthur.House.
[03:18:49 DEBUG The Living Valley] Autonomy: Beckett->Arthur staged successfully, starting conversation.
[03:18:49 DEBUG The Living Valley] Autonomy: Beckett->Arthur Player2 encounter conversation launched (turns=4, continuation=False).
[03:18:50 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:50 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:51 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:51 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:18:51 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:18:51 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:18:51 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:18:51 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:18:51 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:18:51 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:18:51 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:18:51 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:18:51 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:18:51 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:18:51 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:18:51 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:18:51 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:18:51 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:18:51 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:18:51 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:18:51 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:51 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:18:51 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:18:51 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:18:51 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=10.0).
[03:18:51 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:18:51 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:18:51 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:18:51 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=10.0).
[03:18:51 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=10.0).
[03:18:51 TRACE The Living Valley] Autonomy: MarlonFay found target Brock but out of talk range (dist=7.0).
[03:18:51 TRACE The Living Valley] Player2 stream line: {"message":"\u003cbeckett\u003e Olivia’s lantern demo sparked a tremor—might signal a shift near the mine entrance.","npc_id":"af7a5565-e24b-493b-b33a-d87cbf54be4f"}
[03:18:51 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:52 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:52 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:52 TRACE SpaceCore] Event: BeforeWarp
[03:18:52 TRACE game] Warping to Town
[03:18:52 TRACE The Living Valley] Player2 stream line: {"message":"\u003carthur\u003e That tremor's a warning—I'll double‑check the shaft supports today.","npc_id":"32f69a14-a4b7-44f3-9252-982d2d3d6bef"}
[03:18:52 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:53 TRACE Content Patcher] Requested cache invalidation for all assets matching a predicate.
[03:18:53 TRACE SMAPI] Invalidated 1 asset names (LooseSprites/font_bold).
Propagated 1 core assets (LooseSprites/font_bold).
[03:18:53 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:53 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:53 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:54 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:54 TRACE The Living Valley] Player2 stream line: {"message":"\u003cbeckett\u003e I'll bring the geophone and map any new cracks before noon.","npc_id":"af7a5565-e24b-493b-b33a-d87cbf54be4f"}
[03:18:54 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:55 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:55 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:55 TRACE The Living Valley] Player2 stream line: {"message":"\u003carthur\u003e Anyway, keep those ears sharp—let’s catch whatever the earth’s whispering.","npc_id":"32f69a14-a4b7-44f3-9252-982d2d3d6bef"}
[03:18:55 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:18:55 DEBUG The Living Valley] Encounter conversation completed: Beckett->Arthur enc=enc_27 turns=4/4 duration_ms=6619.
[03:18:55 TRACE The Living Valley] Encounter transcript T1 Beckett->Arthur: Olivia’s lantern demo sparked a tremor—might signal a shift near the mine entrance.
[03:18:55 TRACE The Living Valley] Encounter transcript T2 Arthur->Beckett: That tremor's a warning—I'll double‑check the shaft supports today.
[03:18:55 TRACE The Living Valley] Encounter transcript T3 Beckett->Arthur: I'll bring the geophone and map any new cracks before noon.
[03:18:55 TRACE The Living Valley] Encounter transcript T4 Arthur->Beckett: Anyway, keep those ears sharp—let’s catch whatever the earth’s whispering.
[03:18:56 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:56 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:18:56 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:18:56 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:18:56 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:18:56 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:18:56 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:18:56 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:18:56 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:18:56 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:18:56 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:18:56 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:18:56 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:18:56 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:18:56 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:18:56 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:18:56 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:18:56 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:18:56 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:18:56 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:18:56 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:18:56 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:18:56 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=15.0).
[03:18:56 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:18:56 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:18:56 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:18:56 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=10.0).
[03:18:56 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=10.0).
[03:18:56 TRACE The Living Valley] Autonomy: MarlonFay found target Brock but out of talk range (dist=6.0).
[03:18:56 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:18:56 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:57 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:57 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:58 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:58 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:59 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:18:59 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:00 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:00 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:01 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:01 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:19:01 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:19:01 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:01 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:19:01 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:01 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:01 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:01 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:01 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:01 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:19:01 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:19:01 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:19:01 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=113.0).
[03:19:01 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:01 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:01 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:01 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:19:01 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:01 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:19:01 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:01 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:01 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=15.0).
[03:19:01 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:01 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:01 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:01 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=10.0).
[03:19:01 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=10.0).
[03:19:01 TRACE The Living Valley] Autonomy: MarlonFay found target Brock but out of talk range (dist=6.0).
[03:19:01 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:02 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:02 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:03 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:03 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:04 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:04 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:05 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:05 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:06 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:06 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:19:06 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:19:06 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:06 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:19:06 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:06 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:06 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:06 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:06 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:06 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:19:06 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:19:06 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:19:06 TRACE The Living Valley] Autonomy: Marlon found target MarlonFay but out of talk range (dist=118.0).
[03:19:06 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:06 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:06 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:06 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:19:06 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:06 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:19:06 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:06 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:06 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:06 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=8.0).
[03:19:06 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:06 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:06 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:06 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=5.0).
[03:19:06 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=5.0).
[03:19:06 TRACE The Living Valley] Autonomy: MarlonFay found target Brock but out of talk range (dist=5.0).
[03:19:06 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:07 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:07 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:08 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:08 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:09 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:19:09 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:19:10 TRACE The Living Valley] Autonomy: encounter enc_27 Beckett->Arthur waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:19:10 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Beckett->Arthur after complete.
[03:19:10 DEBUG The Living Valley] Autonomy: [HANDOFF] Beckett starting handoff: TilePoint=(8,7), controller=null, followSchedule=True, time=640, map=DH.Arthur.House.
[03:19:10 TRACE The Living Valley] Autonomy: queued Beckett for vanilla schedule resume after encounter enc_27 (complete, restored=False, next_tick=30871, map=DH.Arthur.House, time=640).
[03:19:10 DEBUG The Living Valley] Autonomy: [HANDOFF] Arthur starting handoff: TilePoint=(11,7), controller=null, followSchedule=True, time=640, map=DH.Arthur.House.
[03:19:10 TRACE The Living Valley] Autonomy: queued Arthur for vanilla schedule resume after encounter enc_27 (complete, restored=False, next_tick=30871, map=DH.Arthur.House, time=640).
[03:19:10 DEBUG The Living Valley] Autonomy: Player2 encounter enc_27 Beckett->Arthur completed (outcome=friendly).
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Beckett starting rebind at TilePoint=(8,7), controller=null, followSchedule=True, temporaryController=null, map=DH.Arthur.House, time=640.
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Beckett cleared schedule, calling TryLoadSchedule().
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Beckett TryLoadSchedule returned=True, schedule_count=7, first_keys=620,800,1030,1330,1700.
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Beckett current_time=640, entries_before_current=620:DH.Arthur.House.
[03:19:10 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Beckett already at active-slot destination after encounter enc_27 (active_schedule_time=620, next_schedule_time=800, location=DH.Arthur.House, tile=(8,7), time=640).
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Beckett reset complete: lastAttemptedSchedule=640, previousEndPoint=(8,7), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=620, next_schedule_time=800, active_target_location=DH.Arthur.House, active_target_tile=(8,7), active_facing=0, active_behavior=none, fallback_used=False.
[03:19:10 DEBUG The Living Valley] Autonomy: waiting to return Beckett to vanilla schedule after encounter enc_27 (complete, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=640, active_schedule_time=620, next_schedule_time=800, active_target_location=DH.Arthur.House, active_target_tile=(8,7), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(8,7), previousEndPoint=(8,7), lastAttemptedSchedule=640, map=DH.Arthur.House, time=640).
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Arthur starting rebind at TilePoint=(11,7), controller=null, followSchedule=True, temporaryController=null, map=DH.Arthur.House, time=640.
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Arthur cleared schedule, calling TryLoadSchedule().
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Arthur TryLoadSchedule returned=True, schedule_count=6, first_keys=610,730,1030,1500,1700.
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Arthur current_time=640, entries_before_current=610:DH.Arthur.House.
[03:19:10 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Arthur forced same-map active-slot path after encounter enc_27 (active_schedule_time=610, next_schedule_time=730, location=DH.Arthur.House, tile=(9,7), time=640).
[03:19:10 DEBUG The Living Valley] Autonomy: [REBIND] Arthur reset complete: lastAttemptedSchedule=640, previousEndPoint=(9,7), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=610, next_schedule_time=730, active_target_location=DH.Arthur.House, active_target_tile=(9,7), active_facing=0, active_behavior=none, fallback_used=True.
[03:19:10 DEBUG The Living Valley] Autonomy: [ARRIVAL] Beckett active-slot handoff at tile (8,7) in DH.Arthur.House (active_schedule_time=620, active_facing=0, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(8,7), facing=0, time=640).
[03:19:10 DEBUG The Living Valley] Autonomy: returned Beckett to active-slot schedule action after encounter enc_27 (complete, restored=False, attempts=1, active_schedule_time=620, next_schedule_time=800, active_target_location=DH.Arthur.House, active_target_tile=(8,7), active_facing=0, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(8,7), previousEndPoint=(8,7), lastAttemptedSchedule=640, map=DH.Arthur.House, time=640).
[03:19:10 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_27 tick=1: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:10 DEBUG The Living Valley] Autonomy: [ARRIVAL] Arthur active-slot handoff at tile (9,7) in DH.Arthur.House (active_schedule_time=610, active_facing=0, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(10,7), facing=3, time=640).
[03:19:10 DEBUG The Living Valley] Autonomy: returned Arthur to active-slot schedule action after encounter enc_27 (complete, restored=False, attempts=1, active_schedule_time=610, next_schedule_time=730, active_target_location=DH.Arthur.House, active_target_tile=(9,7), active_facing=0, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(10,7), previousEndPoint=(9,7), lastAttemptedSchedule=640, map=DH.Arthur.House, time=640).
[03:19:11 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_27 tick=2: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:11 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_27 tick=1: controller=PathFindController, isMoving=True, TilePoint=(11,7), moved_from_initial=no, previousEndPoint=(9,7), followSchedule=True.
[03:19:11 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:19:11 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:19:11 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:11 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:19:11 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=79.0).
[03:19:11 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:11 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:11 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:11 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:11 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:11 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:19:11 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:19:11 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:19:11 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:11 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:11 TRACE The Living Valley] Autonomy: MorrisTod found target Clint but out of talk range (dist=79.0).
[03:19:11 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:11 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:11 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:19:11 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:11 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:19:11 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:11 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:11 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:11 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=8.0).
[03:19:11 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:11 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:11 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:11 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=5.0).
[03:19:11 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=5.0).
[03:19:11 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_27 tick=3: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:11 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_27 tick=2: controller=PathFindController, isMoving=True, TilePoint=(11,7), moved_from_initial=no, previousEndPoint=(9,7), followSchedule=True.
[03:19:11 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_27 tick=4: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:11 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_27 tick=3: controller=PathFindController, isMoving=True, TilePoint=(11,7), moved_from_initial=no, previousEndPoint=(9,7), followSchedule=True.
[03:19:11 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_27 tick=5: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:11 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_27 tick=4: controller=PathFindController, isMoving=True, TilePoint=(12,7), moved_from_initial=yes, previousEndPoint=(9,7), followSchedule=True.
[03:19:11 DEBUG The Living Valley] Autonomy: [MONITOR] Arthur encounter=enc_27 tick=5: controller=PathFindController, isMoving=True, TilePoint=(12,7), moved_from_initial=yes, previousEndPoint=(9,7), followSchedule=True.
[03:19:16 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:19:16 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:19:16 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:16 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:19:16 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=71.0).
[03:19:16 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:16 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:16 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:16 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:16 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:16 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:19:16 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:19:16 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:19:16 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:16 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:16 TRACE The Living Valley] Autonomy: MorrisTod found target Clint but out of talk range (dist=71.0).
[03:19:16 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:16 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:16 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:19:16 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:16 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:19:16 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:16 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:16 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:16 TRACE The Living Valley] Autonomy: Jolyne found target Drake but out of talk range (dist=7.0).
[03:19:16 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:16 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:16 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:16 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=5.0).
[03:19:16 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=5.0).
[03:19:18 DEBUG The Living Valley] Autonomy: Jolyne->Drake encounter approved! block=BaseAnchor location=Custom_FirstSlashGuild.
[03:19:18 DEBUG The Living Valley] Autonomy: Jolyne->Drake staged successfully, starting conversation.
[03:19:18 DEBUG The Living Valley] Autonomy: Jolyne->Drake Player2 unavailable — cancelling encounter.
[03:19:18 TRACE The Living Valley] Autonomy: cancelling encounter enc_28 Jolyne->Drake (player2_unavailable).
[03:19:18 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Jolyne->Drake after player2_unavailable.
[03:19:18 DEBUG The Living Valley] Autonomy: [HANDOFF] Jolyne starting handoff: TilePoint=(15,14), controller=null, followSchedule=True, time=650, map=Custom_FirstSlashGuild.
[03:19:18 TRACE The Living Valley] Autonomy: queued Jolyne for vanilla schedule resume after encounter enc_28 (player2_unavailable, restored=False, next_tick=31351, map=Custom_FirstSlashGuild, time=650).
[03:19:18 DEBUG The Living Valley] Autonomy: [HANDOFF] Drake starting handoff: TilePoint=(14,16), controller=null, followSchedule=True, time=650, map=Custom_FirstSlashGuild.
[03:19:18 TRACE The Living Valley] Autonomy: queued Drake for vanilla schedule resume after encounter enc_28 (player2_unavailable, restored=False, next_tick=31351, map=Custom_FirstSlashGuild, time=650).
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Drake starting rebind at TilePoint=(14,16), controller=null, followSchedule=True, temporaryController=null, map=Custom_FirstSlashGuild, time=650.
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Drake cleared schedule, calling TryLoadSchedule().
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Drake TryLoadSchedule returned=True, schedule_count=25, first_keys=610,630,650,710,730.
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Drake current_time=650, entries_before_current=610:Custom_FirstSlashGuild,630:Custom_FirstSlashGuild,650:Custom_FirstSlashGuild.
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Drake reset complete: lastAttemptedSchedule=650, previousEndPoint=(20,16), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=650, next_schedule_time=710, active_target_location=Custom_FirstSlashGuild, active_target_tile=(20,16), active_facing=1, active_behavior=none, fallback_used=False.
[03:19:18 DEBUG The Living Valley] Autonomy: returned Drake to vanilla schedule after encounter enc_28 (player2_unavailable, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=650, active_schedule_time=650, next_schedule_time=710, active_target_location=Custom_FirstSlashGuild, active_target_tile=(20,16), fallback_used=False, resumed=true, method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(22,26), previousEndPoint=(20,16), lastAttemptedSchedule=650, map=Custom_FirstSlashGuild, time=650).
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Jolyne starting rebind at TilePoint=(15,14), controller=null, followSchedule=True, temporaryController=null, map=Custom_FirstSlashGuild, time=650.
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Jolyne cleared schedule, calling TryLoadSchedule().
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Jolyne TryLoadSchedule returned=True, schedule_count=1, first_keys=610.
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Jolyne current_time=650, entries_before_current=610:Custom_FirstSlashGuild.
[03:19:18 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Jolyne already at active-slot destination after encounter enc_28 (active_schedule_time=610, next_schedule_time=none, location=Custom_FirstSlashGuild, tile=(15,14), time=650).
[03:19:18 DEBUG The Living Valley] Autonomy: [REBIND] Jolyne reset complete: lastAttemptedSchedule=650, previousEndPoint=(15,14), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=610, next_schedule_time=none, active_target_location=Custom_FirstSlashGuild, active_target_tile=(15,14), active_facing=2, active_behavior=none, fallback_used=False.
[03:19:18 DEBUG The Living Valley] Autonomy: vanilla schedule for Jolyne has no future slot after encounter enc_28 (player2_unavailable, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=650, active_schedule_time=610, next_schedule_time=none, active_target_location=Custom_FirstSlashGuild, active_target_tile=(15,14), fallback_used=False, resumed=false, controller=null, isMoving=False, temporary_controller=False, TilePoint=(15,14), previousEndPoint=(15,14), lastAttemptedSchedule=650, map=Custom_FirstSlashGuild, time=650).
[03:19:18 DEBUG The Living Valley] Autonomy: [MONITOR] Drake encounter=enc_28 tick=1: controller=PathFindController, isMoving=True, TilePoint=(22,26), moved_from_initial=yes, previousEndPoint=(20,16), followSchedule=True.
[03:19:19 DEBUG The Living Valley] Autonomy: [MONITOR] Drake encounter=enc_28 tick=2: controller=PathFindController, isMoving=True, TilePoint=(21,26), moved_from_initial=yes, previousEndPoint=(20,16), followSchedule=True.
[03:19:19 DEBUG The Living Valley] Autonomy: [MONITOR] Drake encounter=enc_28 tick=3: controller=PathFindController, isMoving=True, TilePoint=(21,26), moved_from_initial=yes, previousEndPoint=(20,16), followSchedule=True.
[03:19:19 DEBUG The Living Valley] Autonomy: [MONITOR] Drake encounter=enc_28 tick=4: controller=PathFindController, isMoving=True, TilePoint=(21,26), moved_from_initial=yes, previousEndPoint=(20,16), followSchedule=True.
[03:19:19 DEBUG The Living Valley] Autonomy: [MONITOR] Drake encounter=enc_28 tick=5: controller=PathFindController, isMoving=True, TilePoint=(20,26), moved_from_initial=yes, previousEndPoint=(20,16), followSchedule=True.
[03:19:21 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=11.0).
[03:19:21 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=11.0).
[03:19:21 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:21 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:19:21 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=71.0).
[03:19:21 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:19:21 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:21 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:21 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:21 DEBUG The Living Valley] Autonomy: Gunther->GuntherSilvian encounter approved! block=Wander location=ArchaeologyHouse.
[03:19:21 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian blocked by wall (no line of sight).
[03:19:21 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:21 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:21 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=18.0).
[03:19:21 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=8.0).
[03:19:21 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:19:21 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:21 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:21 TRACE The Living Valley] Autonomy: MorrisTod found target Clint but out of talk range (dist=71.0).
[03:19:21 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:21 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:21 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=8.0).
[03:19:21 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:21 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=21.0).
[03:19:21 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:21 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:21 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:21 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:21 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:21 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:21 TRACE The Living Valley] Autonomy: Daulton found target Maple but out of talk range (dist=5.0).
[03:19:21 TRACE The Living Valley] Autonomy: Maple found target Daulton but out of talk range (dist=5.0).
[03:19:24 DEBUG The Living Valley] Autonomy: Daulton->Maple encounter approved! block=BaseAnchor location=Custom_DaultonsRoom.
[03:19:24 TRACE The Living Valley] Autonomy: Daulton->Maple blocked by wall (no line of sight).
[03:19:26 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=13.0).
[03:19:26 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=13.0).
[03:19:26 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:26 TRACE The Living Valley] Autonomy: Caroline found target Abigail but out of talk range (dist=25.0).
[03:19:26 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=71.0).
[03:19:26 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:19:26 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:26 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:26 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:26 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:26 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:26 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=14.0).
[03:19:26 TRACE The Living Valley] Autonomy: Kent found target Sam but out of talk range (dist=12.0).
[03:19:26 TRACE The Living Valley] Autonomy: Marnie found target Shane but out of talk range (dist=15.0).
[03:19:26 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:26 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:26 TRACE The Living Valley] Autonomy: MorrisTod found target Clint but out of talk range (dist=71.0).
[03:19:26 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:26 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:26 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=12.0).
[03:19:26 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:26 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=15.0).
[03:19:26 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:26 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:26 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:26 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:26 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:26 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:31 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=20.0).
[03:19:31 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=20.0).
[03:19:31 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:31 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=27.0).
[03:19:31 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=71.0).
[03:19:31 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:19:31 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:31 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:31 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:31 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:31 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:31 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=7.0).
[03:19:31 TRACE The Living Valley] Autonomy: Kent found target Jodi but out of talk range (dist=7.0).
[03:19:31 TRACE The Living Valley] Autonomy: Marnie found target Shane but out of talk range (dist=12.0).
[03:19:31 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:31 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:31 TRACE The Living Valley] Autonomy: MorrisTod found target Clint but out of talk range (dist=71.0).
[03:19:31 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:31 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:31 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=19.0).
[03:19:31 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:31 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=12.0).
[03:19:31 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:31 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:31 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:31 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:31 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:31 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:32 DEBUG The Living Valley] Autonomy: Chloe->Beckett encounter approved! block=BaseAnchor location=DH.Arthur.House.
[03:19:32 DEBUG The Living Valley] Autonomy: Chloe->Beckett staged successfully, starting conversation.
[03:19:32 DEBUG The Living Valley] Autonomy: Chloe->Beckett Player2 encounter conversation launched (turns=4, continuation=False).
[03:19:33 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:33 TRACE The Living Valley] Player2 stream line: {"message":"\u003cchloe\u003e Did you catch the Wizard's firework trick? It could mask some tunnel tremors.","npc_id":"bc35871b-083c-4f2d-8c14-729dacbd8c5f"}
[03:19:33 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:34 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:34 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:19:34 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:34 TRACE The Living Valley] Player2 stream line: {"message":"\u003cbeckett\u003e Those sparks could hide micro‑shifts—I'll log the timing for analysis.","npc_id":"af7a5565-e24b-493b-b33a-d87cbf54be4f"}
[03:19:35 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:35 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:35 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=2s)
[03:19:36 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:36 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=20.0).
[03:19:36 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=20.0).
[03:19:36 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:36 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=27.0).
[03:19:36 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=71.0).
[03:19:36 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:19:36 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:36 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:36 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:36 TRACE The Living Valley] Autonomy: Gus found target Daulton but out of talk range (dist=19.0).
[03:19:36 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:36 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:36 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=12.0).
[03:19:36 TRACE The Living Valley] Autonomy: Kent found target Vincent but out of talk range (dist=12.0).
[03:19:36 TRACE The Living Valley] Autonomy: Marnie found target Shane but out of talk range (dist=5.0).
[03:19:36 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:36 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:36 TRACE The Living Valley] Autonomy: MorrisTod found target Clint but out of talk range (dist=71.0).
[03:19:36 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:36 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:36 TRACE The Living Valley] Autonomy: Sam found target Kent but out of talk range (dist=16.0).
[03:19:36 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:36 TRACE The Living Valley] Autonomy: Shane found target Marnie but out of talk range (dist=5.0).
[03:19:36 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=12.0).
[03:19:36 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:36 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:36 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:36 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:36 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:36 TRACE The Living Valley] Player2 stream line: {"message":"\u003cchloe\u003e If the sparks sync with the quarry tremors, we might pinpoint the loose strata.","npc_id":"bc35871b-083c-4f2d-8c14-729dacbd8c5f"}
[03:19:36 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:37 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:37 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=False, remaining=False, ready_next=True, last_finished=True, displayed=False).
[03:19:37 TRACE The Living Valley] Player2 stream line: {"message":"\u003cbeckett\u003e Anyway, I’ll sync the data and head back to the quarry, Chloe.","npc_id":"af7a5565-e24b-493b-b33a-d87cbf54be4f"}
[03:19:38 DEBUG The Living Valley] Encounter conversation completed: Chloe->Beckett enc=enc_29 turns=4/4 duration_ms=5864.
[03:19:38 TRACE The Living Valley] Encounter transcript T1 Chloe->Beckett: Did you catch the Wizard's firework trick? It could mask some tunnel tremors.
[03:19:38 TRACE The Living Valley] Encounter transcript T2 Beckett->Chloe: Those sparks could hide micro‑shifts—I'll log the timing for analysis.
[03:19:38 TRACE The Living Valley] Encounter transcript T3 Chloe->Beckett: If the sparks sync with the quarry tremors, we might pinpoint the loose strata.
[03:19:38 TRACE The Living Valley] Encounter transcript T4 Beckett->Chloe: Anyway, I’ll sync the data and head back to the quarry, Chloe.
[03:19:38 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:38 DEBUG The Living Valley] Starting Player2 stream listener... (backoff=1s)
[03:19:38 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:39 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:39 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:40 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:40 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:41 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:41 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=20.0).
[03:19:41 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=20.0).
[03:19:41 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:41 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=27.0).
[03:19:41 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=71.0).
[03:19:41 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:19:41 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:41 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:41 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:41 TRACE The Living Valley] Autonomy: Gus found target Daulton but out of talk range (dist=14.0).
[03:19:41 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:41 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:41 TRACE The Living Valley] Autonomy: Jodi found target Kent but out of talk range (dist=15.0).
[03:19:41 TRACE The Living Valley] Autonomy: Kent found target Vincent but out of talk range (dist=9.0).
[03:19:41 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:19:41 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:41 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:41 TRACE The Living Valley] Autonomy: MorrisTod found target Clint but out of talk range (dist=71.0).
[03:19:41 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:41 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:41 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=22.0).
[03:19:41 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:41 TRACE The Living Valley] Autonomy: Vincent found target Kent but out of talk range (dist=9.0).
[03:19:41 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:41 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:41 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:41 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:41 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:41 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:42 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:42 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:43 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:43 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:44 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:44 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:45 TRACE SMAPI] Content Patcher loaded asset 'Characters/Dialogue/Claire' (for the 'Stardew Valley Expanded' content pack).
[03:19:45 TRACE SMAPI] Content Patcher edited Characters/Dialogue/Claire (for the 'Daulton - Marriageable Character' content pack).
[03:19:45 TRACE SMAPI] Content Patcher edited Characters/Dialogue/Claire (for the 'Stardew Valley Expanded' content pack).
[03:19:45 TRACE Farm Type Manager (FTM)] Spawned 1 objects. Time: 730.
[03:19:45 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:45 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:46 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:46 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=20.0).
[03:19:46 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=20.0).
[03:19:46 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:46 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=27.0).
[03:19:46 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=73.0).
[03:19:46 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:19:46 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:46 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:46 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:46 TRACE The Living Valley] Autonomy: Gus found target Daulton but out of talk range (dist=14.0).
[03:19:46 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:46 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:46 TRACE The Living Valley] Autonomy: Jodi found target Vincent but out of talk range (dist=24.0).
[03:19:46 TRACE The Living Valley] Autonomy: Kent found target MorrisTod but out of talk range (dist=65.0).
[03:19:46 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:19:46 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:46 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:46 TRACE The Living Valley] Autonomy: MorrisTod found target Kent but out of talk range (dist=65.0).
[03:19:46 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:46 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:46 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=22.0).
[03:19:46 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:46 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:46 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:46 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:46 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=12.0).
[03:19:46 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:46 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=12.0).
[03:19:46 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:47 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:47 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:48 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:48 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:49 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:49 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:50 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:50 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=True, ready_next=False, last_finished=True, displayed=True).
[03:19:51 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:19:51 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=20.0).
[03:19:51 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=20.0).
[03:19:51 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:51 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=27.0).
[03:19:51 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=76.0).
[03:19:51 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:19:51 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:51 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:51 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:51 TRACE The Living Valley] Autonomy: Gus found target Daulton but out of talk range (dist=14.0).
[03:19:51 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:51 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:51 TRACE The Living Valley] Autonomy: Jodi found target Vincent but out of talk range (dist=24.0).
[03:19:51 TRACE The Living Valley] Autonomy: Kent found target MorrisTod but out of talk range (dist=56.0).
[03:19:51 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:19:51 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:51 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:51 TRACE The Living Valley] Autonomy: MorrisTod found target Kent but out of talk range (dist=56.0).
[03:19:51 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:51 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:51 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=22.0).
[03:19:51 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:51 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:51 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:51 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:51 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=21.0).
[03:19:51 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:51 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=21.0).
[03:19:51 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:19:52 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:19:52 TRACE The Living Valley] Autonomy: encounter enc_29 Chloe->Beckett waiting on Player2 bubbles (ever_queued=True, remaining=False, ready_next=False, last_finished=False, displayed=True).
[03:19:53 TRACE The Living Valley] Autonomy: released vanilla encounter scene for Chloe->Beckett after complete.
[03:19:53 DEBUG The Living Valley] Autonomy: [HANDOFF] Chloe starting handoff: TilePoint=(6,7), controller=null, followSchedule=True, time=740, map=DH.Arthur.House.
[03:19:53 TRACE The Living Valley] Autonomy: queued Chloe for vanilla schedule resume after encounter enc_29 (complete, restored=False, next_tick=33421, map=DH.Arthur.House, time=740).
[03:19:53 DEBUG The Living Valley] Autonomy: [HANDOFF] Beckett starting handoff: TilePoint=(8,7), controller=null, followSchedule=True, time=740, map=DH.Arthur.House.
[03:19:53 TRACE The Living Valley] Autonomy: queued Beckett for vanilla schedule resume after encounter enc_29 (complete, restored=False, next_tick=33421, map=DH.Arthur.House, time=740).
[03:19:53 DEBUG The Living Valley] Autonomy: Player2 encounter enc_29 Chloe->Beckett completed (outcome=friendly).
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Beckett starting rebind at TilePoint=(8,7), controller=null, followSchedule=True, temporaryController=null, map=DH.Arthur.House, time=740.
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Beckett cleared schedule, calling TryLoadSchedule().
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Beckett TryLoadSchedule returned=True, schedule_count=7, first_keys=620,800,1030,1330,1700.
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Beckett current_time=740, entries_before_current=620:DH.Arthur.House.
[03:19:53 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Beckett already at active-slot destination after encounter enc_29 (active_schedule_time=620, next_schedule_time=800, location=DH.Arthur.House, tile=(8,7), time=740).
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Beckett reset complete: lastAttemptedSchedule=740, previousEndPoint=(8,7), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=620, next_schedule_time=800, active_target_location=DH.Arthur.House, active_target_tile=(8,7), active_facing=0, active_behavior=none, fallback_used=False.
[03:19:53 DEBUG The Living Valley] Autonomy: waiting to return Beckett to vanilla schedule after encounter enc_29 (complete, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=740, active_schedule_time=620, next_schedule_time=800, active_target_location=DH.Arthur.House, active_target_tile=(8,7), fallback_used=False, controller=null, isMoving=False, temporary_controller=False, TilePoint=(8,7), previousEndPoint=(8,7), lastAttemptedSchedule=740, map=DH.Arthur.House, time=740).
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Chloe starting rebind at TilePoint=(6,7), controller=null, followSchedule=True, temporaryController=null, map=DH.Arthur.House, time=740.
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Chloe cleared schedule, calling TryLoadSchedule().
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Chloe TryLoadSchedule returned=True, schedule_count=8, first_keys=620,700,830,1030,1330.
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Chloe current_time=740, entries_before_current=620:DH.Arthur.House.ChloeRoom,700:Downhill.
[03:19:53 DEBUG The Living Valley] Autonomy: [CrossMapLeg(start)] Chloe encounter=enc_29 from=DH.Arthur.House to=Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19) arrival_resolved=True active_target_location=Downhill active_target_tile=(29,20) time=740.
[03:19:53 DEBUG The Living Valley] Autonomy: [REBIND] Chloe reset complete: lastAttemptedSchedule=740, previousEndPoint=(12,13), check_schedule_invoked=True, check_schedule_method=checkSchedule(int), active_schedule_time=700, next_schedule_time=830, active_target_location=Downhill, active_target_tile=(29,20), active_facing=2, active_behavior=chloe_coffee, fallback_used=True.
[03:19:53 DEBUG The Living Valley] Autonomy: [ARRIVAL] Beckett active-slot handoff at tile (8,7) in DH.Arthur.House (active_schedule_time=620, active_facing=0, active_behavior=none, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(8,7), facing=0, time=740).
[03:19:53 DEBUG The Living Valley] Autonomy: returned Beckett to active-slot schedule action after encounter enc_29 (complete, restored=False, attempts=1, active_schedule_time=620, next_schedule_time=800, active_target_location=DH.Arthur.House, active_target_tile=(8,7), active_facing=0, active_behavior=none, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(8,7), previousEndPoint=(8,7), lastAttemptedSchedule=740, map=DH.Arthur.House, time=740).
[03:19:53 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_29 tick=1: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:53 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_29 tick=2: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:53 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(7,7) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:53 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_29 tick=3: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:53 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_29 tick=4: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:54 DEBUG The Living Valley] Autonomy: [MONITOR] Beckett encounter=enc_29 tick=5: controller=null, isMoving=False, TilePoint=(8,7), moved_from_initial=no, previousEndPoint=(8,7), followSchedule=True.
[03:19:54 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(8,7) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:54 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(9,7) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:55 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(10,7) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:55 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(11,7) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:56 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=20.0).
[03:19:56 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=20.0).
[03:19:56 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:19:56 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=27.0).
[03:19:56 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=76.0).
[03:19:56 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:19:56 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:19:56 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:19:56 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:19:56 TRACE The Living Valley] Autonomy: Gus found target Daulton but out of talk range (dist=14.0).
[03:19:56 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:19:56 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:19:56 TRACE The Living Valley] Autonomy: Jodi found target Vincent but out of talk range (dist=24.0).
[03:19:56 TRACE The Living Valley] Autonomy: Kent found target MorrisTod but out of talk range (dist=49.0).
[03:19:56 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:19:56 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:19:56 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:19:56 TRACE The Living Valley] Autonomy: MorrisTod found target Kent but out of talk range (dist=49.0).
[03:19:56 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:19:56 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:19:56 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=22.0).
[03:19:56 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:19:56 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:19:56 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:19:56 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:19:56 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=25.0).
[03:19:56 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:19:56 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=25.0).
[03:19:56 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(12,7) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:56 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(12,8) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:57 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(12,9) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:58 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(12,10) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:58 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(12,11) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:59 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(12,12) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:59 DEBUG The Living Valley] Autonomy: [CrossMapLeg(transition_ready)] Chloe encounter=enc_29 map=DH.Arthur.House tile=(12,12) transition_tile=(12,13) approach_tile=(12,13).
[03:19:59 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warping)] Chloe encounter=enc_29 from=DH.Arthur.House to=Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:59 DEBUG The Living Valley] Autonomy: [CrossMapLeg(progress)] Chloe encounter=enc_29 map=Downhill tile=(27,19) target_leg=DH.Arthur.House->Downhill transition_tile=(12,13) approach_tile=(12,13) arrival_tile=(27,19).
[03:19:59 DEBUG The Living Valley] Autonomy: [CrossMapLeg(warped)] Chloe encounter=enc_29 reached Downhill from DH.Arthur.House.
[03:19:59 DEBUG The Living Valley] Autonomy: [CrossMapLeg(target_map)] Chloe encounter=enc_29 reached target map Downhill; switching to active-slot target fallback.
[03:19:59 DEBUG The Living Valley] Autonomy: [FORCE_PATH] Chloe forced same-map active-slot path after encounter enc_29 (active_schedule_time=700, next_schedule_time=830, location=Downhill, tile=(29,20), time=740).
[03:19:59 DEBUG The Living Valley] Autonomy: returned Demetrius to vanilla schedule after encounter enc_26 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=610, active_schedule_time=none, next_schedule_time=750, active_target_location=none, active_target_tile=none, fallback_used=False, resumed=true, method=VanillaSchedule(update), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(19,4), previousEndPoint=(29,9), lastAttemptedSchedule=750, map=ScienceHouse, time=750).
[03:19:59 DEBUG The Living Valley] Autonomy: [MONITOR] Demetrius encounter=enc_26 tick=1: controller=PathFindController, isMoving=True, TilePoint=(19,4), moved_from_initial=no, previousEndPoint=(29,9), followSchedule=True.
[03:20:00 DEBUG The Living Valley] Autonomy: [MONITOR] Demetrius encounter=enc_26 tick=2: controller=PathFindController, isMoving=True, TilePoint=(18,4), moved_from_initial=yes, previousEndPoint=(29,9), followSchedule=True.
[03:20:00 DEBUG The Living Valley] Autonomy: [MONITOR] Demetrius encounter=enc_26 tick=3: controller=PathFindController, isMoving=False, TilePoint=(18,4), moved_from_initial=yes, previousEndPoint=(29,9), followSchedule=True.
[03:20:00 DEBUG The Living Valley] Autonomy: [MONITOR] Demetrius encounter=enc_26 tick=4: controller=PathFindController, isMoving=True, TilePoint=(18,4), moved_from_initial=yes, previousEndPoint=(29,9), followSchedule=True.
[03:20:00 DEBUG The Living Valley] Autonomy: [MONITOR] Demetrius encounter=enc_26 tick=5: controller=PathFindController, isMoving=True, TilePoint=(18,5), moved_from_initial=yes, previousEndPoint=(29,9), followSchedule=True.
[03:20:00 DEBUG The Living Valley] Autonomy: [ARRIVAL] Chloe active-slot handoff at tile (29,20) in Downhill (active_schedule_time=700, active_facing=2, active_behavior=chloe_coffee, degraded_clone=False, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(29,21), facing=1, time=750).
[03:20:00 DEBUG The Living Valley] Autonomy: returned Chloe to active-slot schedule action after encounter enc_29 (complete, restored=False, attempts=1, active_schedule_time=700, next_schedule_time=830, active_target_location=Downhill, active_target_tile=(29,20), active_facing=2, active_behavior=chloe_coffee, arrival_rebind_invoked=True, arrival_rebind_method=checkSchedule(int), controller=PathFindController, isMoving=True, temporary_controller=False, TilePoint=(29,21), previousEndPoint=(29,20), lastAttemptedSchedule=750, map=Downhill, time=750).
[03:20:01 DEBUG The Living Valley] Autonomy: [MONITOR] Chloe encounter=enc_29 tick=1: controller=PathFindController, isMoving=True, TilePoint=(29,21), moved_from_initial=yes, previousEndPoint=(29,20), followSchedule=True.
[03:20:01 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=20.0).
[03:20:01 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=20.0).
[03:20:01 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:20:01 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=27.0).
[03:20:01 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=76.0).
[03:20:01 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=18.0).
[03:20:01 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:20:01 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:20:01 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:20:01 TRACE The Living Valley] Autonomy: Gus found target Daulton but out of talk range (dist=14.0).
[03:20:01 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:20:01 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:20:01 TRACE The Living Valley] Autonomy: Jodi found target Vincent but out of talk range (dist=24.0).
[03:20:01 TRACE The Living Valley] Autonomy: Kent found target Shane but out of talk range (dist=33.0).
[03:20:01 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:20:01 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:20:01 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=18.0).
[03:20:01 TRACE The Living Valley] Autonomy: MorrisTod found target Kent but out of talk range (dist=40.0).
[03:20:01 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:20:01 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:20:01 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=22.0).
[03:20:01 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:20:01 TRACE The Living Valley] Autonomy: Shane found target Kent but out of talk range (dist=33.0).
[03:20:01 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:20:01 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=15.0).
[03:20:01 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:20:01 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:20:01 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=16.0).
[03:20:01 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:20:01 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=16.0).
[03:20:01 DEBUG The Living Valley] Autonomy: [MONITOR] Chloe encounter=enc_29 tick=2: controller=PathFindController, isMoving=True, TilePoint=(29,21), moved_from_initial=yes, previousEndPoint=(29,20), followSchedule=True.
[03:20:01 DEBUG The Living Valley] Autonomy: [MONITOR] Chloe encounter=enc_29 tick=3: controller=PathFindController, isMoving=True, TilePoint=(29,21), moved_from_initial=yes, previousEndPoint=(29,20), followSchedule=True.
[03:20:01 DEBUG The Living Valley] Autonomy: [MONITOR] Chloe encounter=enc_29 tick=4: controller=PathFindController, isMoving=True, TilePoint=(30,21), moved_from_initial=yes, previousEndPoint=(29,20), followSchedule=True.
[03:20:01 DEBUG The Living Valley] Autonomy: [MONITOR] Chloe encounter=enc_29 tick=5: controller=PathFindController, isMoving=True, TilePoint=(30,21), moved_from_initial=yes, previousEndPoint=(29,20), followSchedule=True.
[03:20:06 TRACE The Living Valley] Autonomy: Pierre found target Abigail but out of talk range (dist=20.0).
[03:20:06 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=20.0).
[03:20:06 TRACE The Living Valley] Autonomy: Alex found target George but out of talk range (dist=20.0).
[03:20:06 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=27.0).
[03:20:06 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=76.0).
[03:20:06 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=17.0).
[03:20:06 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:20:06 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:20:06 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:20:06 TRACE The Living Valley] Autonomy: Gunther->GuntherSilvian skipped by 50% encounter gate (block=Wander).
[03:20:06 TRACE The Living Valley] Autonomy: Gus found target Daulton but out of talk range (dist=14.0).
[03:20:06 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:20:06 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:20:06 TRACE The Living Valley] Autonomy: Jodi found target Vincent but out of talk range (dist=24.0).
[03:20:06 TRACE The Living Valley] Autonomy: Kent found target Shane but out of talk range (dist=33.0).
[03:20:06 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:20:06 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:20:06 TRACE The Living Valley] Autonomy: Maru found target Demetrius but out of talk range (dist=17.0).
[03:20:06 TRACE The Living Valley] Autonomy: MorrisTod found target Kent but out of talk range (dist=39.0).
[03:20:06 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:20:06 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:20:06 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=22.0).
[03:20:06 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:20:06 TRACE The Living Valley] Autonomy: Shane found target Kent but out of talk range (dist=33.0).
[03:20:06 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:20:06 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=27.0).
[03:20:06 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=31.0).
[03:20:06 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:20:06 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=25.0).
[03:20:06 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=31.0).
[03:20:06 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=25.0).
[03:20:06 DEBUG The Living Valley] Autonomy: GuntherSilvian->Gunther encounter approved! block=BaseAnchor location=ArchaeologyHouse.
[03:20:06 TRACE The Living Valley] Autonomy: GuntherSilvian->Gunther blocked by wall (no line of sight).
[03:20:07 DEBUG The Living Valley] Autonomy: returned Robin to vanilla schedule after encounter enc_26 (ui_interrupt, restored=False, attempts=1, check_schedule_invoked=True, check_schedule_method=checkSchedule(int), last_attempt_time=610, active_schedule_time=none, next_schedule_time=800, active_target_location=none, active_target_tile=none, fallback_used=False, resumed=true, method=VanillaSchedule(update), controller=PathFindController, isMoving=False, temporary_controller=False, TilePoint=(21,4), previousEndPoint=(8,18), lastAttemptedSchedule=800, map=ScienceHouse, time=800).
[03:20:07 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_26 tick=1: controller=PathFindController, isMoving=True, TilePoint=(21,4), moved_from_initial=no, previousEndPoint=(8,18), followSchedule=True.
[03:20:07 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_26 tick=2: controller=PathFindController, isMoving=True, TilePoint=(20,4), moved_from_initial=yes, previousEndPoint=(8,18), followSchedule=True.
[03:20:07 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_26 tick=3: controller=PathFindController, isMoving=False, TilePoint=(20,4), moved_from_initial=yes, previousEndPoint=(8,18), followSchedule=True.
[03:20:07 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_26 tick=4: controller=PathFindController, isMoving=True, TilePoint=(20,4), moved_from_initial=yes, previousEndPoint=(8,18), followSchedule=True.
[03:20:07 DEBUG The Living Valley] Autonomy: [MONITOR] Robin encounter=enc_26 tick=5: controller=PathFindController, isMoving=True, TilePoint=(19,4), moved_from_initial=yes, previousEndPoint=(8,18), followSchedule=True.
[03:20:11 TRACE The Living Valley] Autonomy: Pierre found target Caroline but out of talk range (dist=19.0).
[03:20:11 TRACE The Living Valley] Autonomy: Abigail found target Pierre but out of talk range (dist=20.0).
[03:20:11 TRACE The Living Valley] Autonomy: Alex found target Evelyn but out of talk range (dist=21.0).
[03:20:11 TRACE The Living Valley] Autonomy: Caroline found target Pierre but out of talk range (dist=19.0).
[03:20:11 TRACE The Living Valley] Autonomy: Clint found target MorrisTod but out of talk range (dist=72.0).
[03:20:11 TRACE The Living Valley] Autonomy: Demetrius found target Maru but out of talk range (dist=21.0).
[03:20:11 TRACE The Living Valley] Autonomy: Emily found target Haley but out of talk range (dist=10.0).
[03:20:11 TRACE The Living Valley] Autonomy: Evelyn found target George but out of talk range (dist=19.0).
[03:20:11 TRACE The Living Valley] Autonomy: George found target Evelyn but out of talk range (dist=19.0).
[03:20:11 TRACE The Living Valley] Autonomy: Gus found target Daulton but out of talk range (dist=18.0).
[03:20:11 TRACE The Living Valley] Autonomy: Haley found target Emily but out of talk range (dist=10.0).
[03:20:11 TRACE The Living Valley] Autonomy: Jas found target Marnie but out of talk range (dist=16.0).
[03:20:11 TRACE The Living Valley] Autonomy: Jodi found target Sam but out of talk range (dist=24.0).
[03:20:11 TRACE The Living Valley] Autonomy: Kent found target Shane but out of talk range (dist=30.0).
[03:20:11 TRACE The Living Valley] Autonomy: Marnie found target Jas but out of talk range (dist=16.0).
[03:20:11 TRACE The Living Valley] Autonomy: Marlon found target Brock but out of talk range (dist=119.0).
[03:20:11 TRACE The Living Valley] Autonomy: Maru found target Robin but out of talk range (dist=12.0).
[03:20:11 TRACE The Living Valley] Autonomy: MorrisTod found target Shane but out of talk range (dist=51.0).
[03:20:11 TRACE The Living Valley] Autonomy: Pam found target Penny but out of talk range (dist=16.0).
[03:20:11 TRACE The Living Valley] Autonomy: Penny found target Pam but out of talk range (dist=16.0).
[03:20:11 TRACE The Living Valley] Autonomy: Sam found target Vincent but out of talk range (dist=22.0).
[03:20:11 TRACE The Living Valley] Autonomy: Sandy found target Bouncer but out of talk range (dist=17.0).
[03:20:11 TRACE The Living Valley] Autonomy: Shane found target Kent but out of talk range (dist=30.0).
[03:20:11 TRACE The Living Valley] Autonomy: Vincent found target Sam but out of talk range (dist=22.0).
[03:20:11 TRACE The Living Valley] Autonomy: Morrow found target Claire but out of talk range (dist=14.0).
[03:20:11 TRACE The Living Valley] Autonomy: Arthur found target Chloe but out of talk range (dist=20.0).
[03:20:11 TRACE The Living Valley] Autonomy: Claire found target Morrow but out of talk range (dist=14.0).
[03:20:11 TRACE The Living Valley] Autonomy: HankSVE found target Treyvon but out of talk range (dist=23.0).
[03:20:11 TRACE The Living Valley] Autonomy: Jadu found target Sludge but out of talk range (dist=17.0).
[03:20:11 TRACE The Living Valley] Autonomy: Olivia found target Victor but out of talk range (dist=25.0).
[03:20:11 TRACE The Living Valley] Autonomy: Treyvon found target HankSVE but out of talk range (dist=23.0).
[03:20:11 TRACE The Living Valley] Autonomy: Victor found target Olivia but out of talk range (dist=25.0).
[03:20:11 TRACE The Living Valley] Autonomy: Daulton found target MarchFoM but out of talk range (dist=17.0).
[03:20:11 TRACE The Living Valley] Autonomy: MarchFoM found target Daulton but out of talk range (dist=17.0).
