namespace Pharos.Server.Result.CurveFit.PiePuf;

internal class CurveChecker
{
    private const int MAX_STANDARD = 15;     /* Max number of calibrator points */
    private const int MAX_REPL = 5;    /* Max number of replicates */
    private const int METHOD_MAXSTDREPL = MAX_REPL;

    private const double METHOD_MINRESP = 0.0;
    private const double METHOD_MAXRESP = 99999.0;

    private const int ACCEPTANCE_NG = 0;
    private const int ACCEPTANCE_OK = 2;

    private const int MAX_CURVE_POINTS = 400;
    private const int MAX_CURVE_LEVELS = 20;

    public static int CurveCheck(CurveType curve, int straightLineSwitch)
    {
        // Set data into the method structure
        var method = new tagMETHOD(MAX_STANDARD)
        {
            calculation_zeropoint_switch = straightLineSwitch,
            no_stdpoints = curve.NumberOfLevels,
            std_maxbad = curve.MaxBadReplicates,
            ExcludeLowestCalibratorFromMeanDiff = curve.ExcludeLowestCalibratorFromMeanDiff
        };

        for (var i = 0; i < curve.NumberOfLevels; i++)
        {
            method.std_point[i] = new tagSTDPOINT
            {
                IdealResponse = curve.IdealResponse[i],
                ExpectedSD = curve.ExpectedSD[i],
                UpperResponseLimit = curve.UpperResponseLimit[i],
                LowerResponseLimit = curve.LowerResponseLimit[i],
                LimitS_Precision = curve.PrecisionSDLimit[i],
                LimitS_Deviation = curve.DeviationSDLimit[i],
                UpperLogarithmicLimit = curve.UpperLogarithmicLimit[i],
                LowerLogarithmicLimit = curve.LowerLogarithmicLimit[i]
            };
        }

        // Set data into the stdcurve structure
        var stdcurve = new tagSTDCURVE(MAX_STANDARD, METHOD_MAXSTDREPL)
        {
            npoints = curve.NumberOfLevels,
            shapeerror_CV = false,
            shapeerror_flatcurve = false,
            shapeerror_lowerparthigh = false,
            shapeerror_singleabove = false,
            shapeerror_singlebelow = false,
            shapeerror_norisingtrend = false
        };

        for (var i = 0; i < MAX_STANDARD; i++)
        {
            stdcurve.ndataorg[i] = 0;
            stdcurve.ndata[i] = 0;
            stdcurve.ndata_in[i] = 0;
            for (var j = 0; j < METHOD_MAXSTDREPL; j++)
            {
                stdcurve.resp[i, j] = 0;
                stdcurve.resporg[i, j] = 0;
            }
        }

        for (var i = 0; i < curve.NumberOfPoints; i++)
        {
            var level = curve.LevelIndex[i] - 1;
            stdcurve.resp[level, stdcurve.ndata[level]] = curve.Response[i];
            stdcurve.resporg[level, stdcurve.ndata[level]] = curve.Response[i];
            stdcurve.ndataorg[level]++;
            stdcurve.ndata[level]++;
            stdcurve.ndata_in[level]++;
        }

        for (var i = 0; i < MAX_CURVE_POINTS; i++)
        {
            curve.FlaggedPoint[i] = 0;
        }
        for (var i = 0; i < MAX_CURVE_LEVELS; i++)
        {
            curve.ArtificialResponse[i] = 0;
            curve.ArtificialLevelFlg[i] = 0;
        }

        var acceptance = DE_CheckCalibrators(method, stdcurve);

        curve.NumberOfFlaggedPoints = 0;
        curve.NumberOfArtificialLevels = 0;
        for (var i = 0; i < stdcurve.npoints; i++)
        {
            var flagged = stdcurve.ndataorg[i] - stdcurve.ndata[i];
            curve.NumberOfFlaggedPoints += flagged;
            if (stdcurve.ndataorg[i] > stdcurve.ndata_in[i])
            {
                curve.ArtificialLevelFlg[i] = 1;
                curve.ArtificialResponse[i] = stdcurve.resp[i, 0];
                curve.NumberOfArtificialLevels++;
            }

            if (flagged > 0)
            {
                for (var j = stdcurve.ndata[i]; j < stdcurve.ndataorg[i]; j++)
                {
                    var flaggedResp = stdcurve.resp[i, j];
                    for (var p = 0; p < curve.NumberOfPoints; p++)
                    {
                        if (curve.Response[p] == flaggedResp && curve.LevelIndex[p] - 1 == i)
                        {
                            curve.FlaggedPoint[p] = 1;
                        }
                    }
                }
            }
        }

        // Check if lowest point is substituted
        if (stdcurve.ndataorg[straightLineSwitch] > stdcurve.ndata_in[straightLineSwitch])
        {
            curve.LowestPointSubstituted = 1;
        }
        else
        {
            curve.LowestPointSubstituted = 0;
        }

        curve.ShapeErrorCV = 0;
        curve.ShapeErrorFlatCurve = 0;
        curve.ShapeErrorLowerPartHigh = 0;
        curve.ShapeErrorSingleAbove = 0;
        curve.ShapeErrorSingleBelow = 0;
        curve.ShapeErrorNoRisingTrend = 0;

        if (stdcurve.shapeerror_CV)
        {
            curve.ShapeErrorCV = 1;
        }

        if (stdcurve.shapeerror_flatcurve)
        {
            curve.ShapeErrorFlatCurve = 1;
        }

        if (stdcurve.shapeerror_lowerparthigh)
        {
            curve.ShapeErrorLowerPartHigh = 1;
        }

        if (stdcurve.shapeerror_singleabove)
        {
            curve.ShapeErrorSingleAbove = 1;
        }

        if (stdcurve.shapeerror_singlebelow)
        {
            curve.ShapeErrorSingleBelow = 1;
        }

        if (stdcurve.shapeerror_norisingtrend)
        {
            curve.ShapeErrorNoRisingTrend = 1;
        }

        if (acceptance == ACCEPTANCE_OK)
        {
            return 1;
        }

        return 0;
    }

    private static int DE_CheckCalibrators(tagMETHOD method, tagSTDCURVE stdcurve)
    {
        var normdose_flg = new bool[MAX_STANDARD, METHOD_MAXSTDREPL];
        var precision_flg = new bool[MAX_STANDARD];
        var deviation_flg = new bool[MAX_STANDARD, METHOD_MAXSTDREPL];
        var resp_est = new double[MAX_STANDARD];
        var resp_best = new double[MAX_STANDARD];
        var resp_best_flg = new bool[MAX_STANDARD];
        var rel_resp = new double[MAX_STANDARD];
        var no_normdoseOK = new int[MAX_STANDARD];
        var resp_normdoseOK = new double[MAX_STANDARD, METHOD_MAXSTDREPL];

        for (var point = 0; point < MAX_STANDARD; point++)
        {
            resp_est[point] = 0.0;
            resp_best[point] = 0.0;
            resp_best_flg[point] = false;
            rel_resp[point] = 0.0;
            precision_flg[point] = false;
            no_normdoseOK[point] = 0;
            for (var replicate = 0; replicate < METHOD_MAXSTDREPL; replicate++)
            {
                normdose_flg[point, replicate] = false;
                deviation_flg[point, replicate] = false;
                resp_normdoseOK[point, replicate] = 0.0;
            }
        }

        var normdose_upper = new double[MAX_STANDARD];
        var normdose_lower = new double[MAX_STANDARD];

        var n_flagged = 0;
        for (var point = 0; point < stdcurve.npoints; point++)
        {
            normdose_upper[point] = METHOD_MAXRESP;
            normdose_lower[point] = METHOD_MINRESP;

            if (method.std_point[point].UpperResponseLimit > 0.05)
            {
                normdose_upper[point] = method.std_point[point].UpperResponseLimit;
            }

            if (method.std_point[point].LowerResponseLimit > 0.05)
            {
                normdose_lower[point] = method.std_point[point].LowerResponseLimit;
            }

            for (var repl = 0; repl < stdcurve.ndataorg[point]; repl++)
            {
                if (stdcurve.resporg[point, repl] > normdose_upper[point] || stdcurve.resporg[point, repl] < normdose_lower[point])
                {
                    normdose_flg[point, repl] = true;
                    n_flagged += 1;
                    Logger.Log("     -. Replicate {Replicate} bad!", repl + 1);
                    //sprintf(DebugBuffer, "     -. Replicate %d bad!", repl + 1);
                    //DEdebug_Info(3, DebugBuffer);
                }
            }
        }

        bool ideal_curve_loop;

        // Iterate and eliminate points based on ideal-curve
        if (n_flagged < 3)
        {
            ideal_curve_loop = true;
        }
        else
        {
            ideal_curve_loop = false;
            //DEdebug_Info(2, "No Ideal-curve check at all, since >= 3 repl removed by Absolute limits");
            Logger.Log("No Ideal-curve check at all, since >= 3 repl removed by Absolute limits");
        }

        while (ideal_curve_loop)
        {
            //DEdebug_Info(2, "Iteration to check against Ideal-curve..");
            Logger.Log("Iteration to check against Ideal-curve..");

            var n_mean_difference = 0;
            var mean_difference = 0.0;

            for (var point = method.calculation_zeropoint_switch; point < stdcurve.npoints; point++)
            {
                if (method.ExcludeLowestCalibratorFromMeanDiff == 1 && point == 0)
                {
                    continue;
                }

                if (method.std_point[point].IdealResponse > 0.05)
                {
                    var ideal_curve = method.std_point[point].IdealResponse;

                    for (var repl = 0; repl < stdcurve.ndataorg[point]; repl++)
                    {
                        if (normdose_flg[point, repl] == false)
                        {
                            mean_difference += Log(stdcurve.resporg[point, repl] / ideal_curve);
                            n_mean_difference++;
                        }
                    }
                }
            }

            if (n_mean_difference > 0)
            {
                mean_difference /= n_mean_difference;
            }

            //sprintf(DebugBuffer, "Mean Ln-Diff=%0.5lf (based on %d replicates)", mean_difference, n_mean_difference);
            //DEdebug_Info(3, DebugBuffer);
            Logger.Log("Mean Ln-Diff={Diff} (based on {NoOfRepl} replicates)", mean_difference, n_mean_difference);

            var max_difference = 0.0;
            var max_difference_point = -1;
            var max_difference_repl = -1;
            for (var point = method.calculation_zeropoint_switch; point < stdcurve.npoints; point++)
            {
                if (method.std_point[point].IdealResponse > 0.05)
                {
                    var ideal_curve = method.std_point[point].IdealResponse;
                    var ideal_lower = mean_difference + method.std_point[point].LowerLogarithmicLimit;
                    var ideal_upper = mean_difference + method.std_point[point].UpperLogarithmicLimit;

                    for (var repl = 0; repl < stdcurve.ndataorg[point]; repl++)
                    {
                        if (normdose_flg[point, repl] == false)
                        {
                            var this_difference = Log(stdcurve.resporg[point, repl] / ideal_curve);
                            //sprintf(DebugBuffer, "Checking Repl %d/%d: Ln-Diff=%0.5lf (%0.5lf .. %0.5lf)", point + 1, repl + 1, this_difference, ideal_lower, ideal_upper);
                            //DEdebug_Info(3, DebugBuffer);
                            Logger.Log("Checking Repl {Repl1}/{Repl2}: Ln-Diff={Diff} ({Range1} .. {Range2})", point + 1, repl + 1, this_difference, ideal_lower, ideal_upper);
                            if (this_difference < ideal_lower || this_difference > ideal_upper)
                            {
                                //sprintf(DebugBuffer, "Got temporary flag, difference from mean Ln-Diff = %0.5lf", fabs(this_difference - mean_difference));
                                //DEdebug_Info(3, DebugBuffer);
                                var diff = Fabs(this_difference - mean_difference);
                                Logger.Log("Got temporary flag, difference from mean Ln-Diff = {Diff}", diff);
                                if (Fabs(this_difference - mean_difference) > max_difference)
                                {
                                    max_difference_point = point;
                                    max_difference_repl = repl;
                                    max_difference = Fabs(this_difference - mean_difference);
                                }
                            }
                        }
                    }
                }
            }

            if (max_difference_point == -1)
            {
                //DEdebug_Info(3, "No temporary flags in this iteration, no more loops!");
                Logger.Log("No temporary flags in this iteration, no more loops!");
                ideal_curve_loop = false;
            }
            else
            {
                //sprintf(DebugBuffer, "Removing worst temporary flagged Repl %d/%d", max_difference_point + 1, max_difference_repl + 1);
                //DEdebug_Info(3, DebugBuffer);
                Logger.Log("Removing worst temporary flagged Repl {Repl1}/{Repl2}", max_difference_point + 1, max_difference_repl + 1);
                normdose_flg[max_difference_point, max_difference_repl] = true;
                n_flagged++;

                if (n_flagged > method.std_maxbad)
                {
                    //DEdebug_Info(3, "Maximum number of replicates removed, no more loops!");
                    Logger.Log("Maximum number of replicates removed, no more loops!");
                    ideal_curve_loop = false;
                }
            }
        }


        // Copy unflagged replicates for future use in Precision
        for (var point = 0; point < stdcurve.npoints; point++)
        {
            for (var replicate = 0; replicate < stdcurve.ndataorg[point]; replicate++)
            {
                if (normdose_flg[point, replicate] == false)
                {
                    resp_normdoseOK[point, no_normdoseOK[point]] = stdcurve.resporg[point, replicate];
                    no_normdoseOK[point]++;
                }
            }
        }


        int flatcurve_point;
        double flatcurve_limit;

        // Find the reference to be used when determining FLAT CURVE
        if (stdcurve.npoints % 2 == 0)
        {
            flatcurve_point = (stdcurve.npoints / 2) - 1;
            if (flatcurve_point < 0)
            {
                flatcurve_point = 0;
            }

            flatcurve_limit = normdose_upper[flatcurve_point];
        }
        else
        {
            flatcurve_point = stdcurve.npoints / 2;

            if (method.std_point[flatcurve_point].IdealResponse > 0.05)
            {
                flatcurve_limit = method.std_point[flatcurve_point].IdealResponse;
            }
            else
            {
                flatcurve_limit = METHOD_MAXRESP;  /* Thus flat curve will not happen */
            }
        }
        //sprintf(buffer, "    Flat curve reference=%d, limit became=%0.1lf", flatcurve_point + 1, flatcurve_limit);

        // Check against FLAT CURVE
        stdcurve.shapeerror_flatcurve = true;
        for (var point = 0; point < stdcurve.npoints; point++)
        {
            for (var replicate = 0; replicate < stdcurve.ndataorg[point]; replicate++)
            {
                if (stdcurve.resporg[point, replicate] < flatcurve_limit)
                {
                    stdcurve.shapeerror_flatcurve = false;
                }
            }
        }
        for (var replicate = 0; replicate < stdcurve.ndataorg[stdcurve.npoints - 1]; replicate++)
        {
            if (stdcurve.resporg[stdcurve.npoints - 1, replicate] > normdose_upper[stdcurve.npoints - 1])
            {
                stdcurve.shapeerror_flatcurve = false;
            }
        }

        if (stdcurve.shapeerror_flatcurve)
        {
            //DEBUG("    FLAT CURVE!");
            Logger.Log("    FLAT CURVE!");
        }

        // Check against LOWER PART TOO HIGH
        if (stdcurve.ndataorg[0] > 0)
        {
            stdcurve.shapeerror_lowerparthigh = true;
        }
        else
        {
            stdcurve.shapeerror_lowerparthigh = false;
        }
        for (var replicate = 0; replicate < stdcurve.ndataorg[0]; replicate++)
        {
            if (stdcurve.resporg[0, replicate] < normdose_upper[0])
            {
                stdcurve.shapeerror_lowerparthigh = false;
            }
        }

        if (stdcurve.shapeerror_lowerparthigh)
        {
            Logger.Log("    LOWER PART HIGH!");
        }

        // Check against SINGLE CALIBRATOR ABOVE and BELOW RANGE
        for (var point = 0; point < stdcurve.npoints; point++)
        {
            var above_flg = true;
            var below_flg = true;
            for (var replicate = 0; replicate < stdcurve.ndataorg[point]; replicate++)
            {
                if (stdcurve.resporg[point, replicate] < normdose_upper[point])
                {
                    above_flg = false;
                }

                if (stdcurve.resporg[point, replicate] > normdose_lower[point])
                {
                    below_flg = false;
                }
            }
            if (stdcurve.ndataorg[point] > 0 && above_flg == true)
            {
                stdcurve.shapeerror_singleabove = true;
                //DEBUG_I("    SINGLE CALIBRATOR ABOVE RANGE, n=", point + 1);
                Logger.Log("    SINGLE CALIBRATOR ABOVE RANGE, n={Point}", point + 1);
            }
            if (stdcurve.ndataorg[point] > 0 && below_flg == true)
            {
                stdcurve.shapeerror_singlebelow = true;
                //DEBUG_I("    SINGLE CALIBRATOR BELOW RANGE, n=", point + 1);
                Logger.Log("    SINGLE CALIBRATOR BELOW RANGE, n={Point}", point + 1);
            }
        }


        double upper;
        // Check against Precision limits
        for (var point = 0; point < stdcurve.npoints; point++)
        {
            if (no_normdoseOK[point] > 0)
            {
                var precision = new double[MAX_STANDARD];
                precision[point] = DE_StdDev(no_normdoseOK[point], GetRow(resp_normdoseOK, point));

                upper = METHOD_MAXRESP;

                if (method.std_point[point].ExpectedSD > 0.05 && method.std_point[point].LimitS_Precision > 0.0005)
                {
                    upper = method.std_point[point].ExpectedSD * method.std_point[point].LimitS_Precision;
                }

                //sprintf(DebugBuffer, "Point %d: Maximum precision    = %0.1lf", point + 1, upper);
                //DEdebug_Info(3, DebugBuffer);
                Logger.Log("Point {Point}: Maximum precision    = {Upper}", point + 1, upper);
                //sprintf(DebugBuffer, "         Calculated precision = %0.1lf", precision[point]);
                //DEdebug_Info(3, DebugBuffer);
                Logger.Log("         Calculated precision = {Precision}", precision[point]);
                if (precision[point] > upper)
                {
                    stdcurve.shapeerror_CV = true;  /* Added 000318 */
                    //DEBUG_I("    CV TOO HIGH, n=", point + 1);
                    Logger.Log("    CV TOO HIGH, n={Point}", point + 1);

                    precision_flg[point] = true;
                    //sprintf(DebugBuffer, "     -. Precision bad!", repl + 1);
                    //DEdebug_Info(3, DebugBuffer);
                    Logger.Log("     -. Precision bad! {Point}", point + 1);
                }
            }
            else
            {
                precision_flg[point] = true;
                //sprintf(DebugBuffer, "Point %d: All replicates deleted", point + 1);
                //DEdebug_Info(3, DebugBuffer);
                Logger.Log("Point {Point}: All replicates deleted", point + 1);
                //DEdebug_Info(3, "     -. Precision bad!");
                Logger.Log("     -. Precision bad!");
            }
        }


        // Calculate relative response for levels not flagged for Precision
        var fatal_flg = true;
        for (var point = 0; point < stdcurve.npoints; point++)
        {
            if (precision_flg[point] == false)
            {
                if (method.std_point[point].IdealResponse > 0.05)
                {
                    rel_resp[point] = DE_Mean(no_normdoseOK[point], GetRow(resp_normdoseOK, point)) / method.std_point[point].IdealResponse;
                }
                else
                {
                    rel_resp[point] = 1.0;
                }

                fatal_flg = false;
            }
        }


        var artificial_flg = false;
        // Give up here if all levels flagged for Precision, delete norm-dose flagged replicates
        if (fatal_flg)
        {
            for (var point = 0; point < stdcurve.npoints; point++)
            {
                DE_InactivateRepl(point, stdcurve, GetRow(normdose_flg, point), GetRow(deviation_flg, point), resp_best_flg[point], resp_best[point], ref artificial_flg, false);
            }

            Logger.Log("Give up here since all levels flagged for Precision. Norm-dose flagged replicates deleted");
            return ACCEPTANCE_NG;
        }


        // Estimated and best response for levels flagged for Precision
        for (var point = 0; point < stdcurve.npoints; point++)
        {
            if (method.std_point[point].IdealResponse < 0.05 || method.std_point[point].ExpectedSD < 0.05)
            {
                continue;
            }

            if (precision_flg[point])
            {
                // Calculate estimated response
                var totalWeight = 0.0;
                int i;
                for (i = 0; i < stdcurve.npoints; i++)
                {
                    if (method.ExcludeLowestCalibratorFromMeanDiff == 1 && i == 0)
                    {
                        continue;
                    }

                    if (point == i)
                    {
                        continue;
                    }

                    if (precision_flg[i])
                    {
                        continue;
                    }

                    var weight = 1 / (double)Abs(i - point);
                    totalWeight += weight;
                    resp_est[point] += rel_resp[i] * weight;
                }
                resp_est[point] /= totalWeight;
                resp_est[point] *= method.std_point[point].IdealResponse;
                //sprintf(DebugBuffer, "Point %d: Estimated response = %0.1lf", point + 1, resp_est[point]);
                //DEdebug_Info(3, DebugBuffer);
                Logger.Log("Point {Point}: Estimated response = {Estimated}", point + 1, resp_est[point]);

                // Find best response
                if (no_normdoseOK[point] == 0)
                {
                    resp_best[point] = resp_est[point];
                    //DEdebug_Info(3, "         Artificial point created!");
                    Logger.Log("         Artificial point created!");
                }
                else
                {
                    resp_best[point] = resp_normdoseOK[point, 0];
                    if (no_normdoseOK[point] > 1)
                    {
                        for (var replicate = 0; replicate < no_normdoseOK[point]; replicate++)
                        {
                            if (Fabs(resp_normdoseOK[point, replicate] - resp_est[point]) <
                                Fabs(resp_best[point] - resp_est[point]))
                            {
                                resp_best[point] = resp_normdoseOK[point, replicate];
                            }
                        }
                    }
                    //sprintf(DebugBuffer, "         Best response      = %0.1lf", resp_best[point]);
                    //DEdebug_Info(3, DebugBuffer);
                    Logger.Log("         Best response      = {Response}", resp_best[point]);

                    upper = method.std_point[point].ExpectedSD * method.std_point[point].LimitS_Deviation;
                    for (var replicate = 0; replicate < no_normdoseOK[point]; replicate++)
                    {
                        if (Fabs(resp_normdoseOK[point, replicate] - resp_best[point]) > upper)
                        {
                            deviation_flg[point, replicate] = true;
                            normdose_flg[point, replicate] = true;
                            //sprintf(DebugBuffer, "     -. Replicate %d bad deviation!", repl + 1);
                            Logger.Log("     -. Replicate {Replicate} bad deviation!", replicate + 1);
                        }
                    }
                }

                resp_best_flg[point] = true;
            }
        }


        // Make an extra check to count number of bad replicates to skip Artificial Responses
        var n_bad = 0;
        var skip_artificial_flg = false;
        for (var point = 0; point < stdcurve.npoints; point++)
        {
            for (var replicate = 0; replicate < stdcurve.ndataorg[point]; replicate++)
            {
                if (normdose_flg[point, replicate] == true || deviation_flg[point, replicate] == true)
                {
                    n_bad++;
                }
            }
        }

        if (method.std_maxbad > 0 && n_bad > method.std_maxbad)
        {
            skip_artificial_flg = true;
            //DEdebug_Info(2, "Number of bar replicates above method limit, skipping Artificial responses");
            Logger.Log("Number of bar replicates above method limit, skipping Artificial responses");
        }

        // Delete flagged replicates
        artificial_flg = false;
        n_bad = 0;
        for (var point = 0; point < stdcurve.npoints; point++)
        {
            n_bad += DE_InactivateRepl(point, stdcurve, GetRow(normdose_flg, point), GetRow(deviation_flg, point), resp_best_flg[point], resp_best[point], ref artificial_flg, skip_artificial_flg);
        }

        // Decide final status of the calibrator data
        var acceptance = ACCEPTANCE_OK;
        if (n_bad > 1)
        {
            acceptance = ACCEPTANCE_NG;
        }

        if (artificial_flg == true)
        {
            acceptance = ACCEPTANCE_NG;
        }

        return acceptance;
    }

    private static double DE_StdDev(int ndata, double[] data)
    {
        if (ndata < 2)
        {
            return 0.0;
        }

        var Ex2 = 0.0;
        var Ex = 0.0;
        for (var i = 0; i < ndata; i++)
        {
            Ex += data[i];
            Ex2 += data[i] * data[i];
        }

        var s = ((ndata * Ex2) - (Ex * Ex)) / (ndata * (ndata - 1.0));
        if (s > 0.0)
        {
            s = Sqrt(s);
        }
        else
        {
            s = 0.0;
        }

        return s;
    }

    private static double DE_Mean(int ndata, double[] data)
    {
        if (ndata < 1)
        {
            return 0.0;
        }

        var result = 0.0;
        for (var i = 0; i < ndata; i++)
        {
            result += data[i];
        }

        return result / ndata;
    }


    /******************************************************************************
    FUNCTION: DE_InactivateRepl

    INPUT:    point          - The point number
              stdcurve       - The standard curve to inactivate replicates in
              normdose_flg   - Normdose flags for each replicate
              deviation_flg  - Deviation flags for each replicate
              resp_best_flg  - True if a calculated response is available
              resp_best      - Calculated response (if resp_best_flg is True)
              skip_artificial_flg - True if artificial responses should not be inserted

    I/O:      artificial_flg - This flag is set True if a replicate was "created"

    RETURNS:  Number of inactivated replicates
    ------------------------------------------------------------------------------
    This function will inactivate replicates flagged for normdose or deviation.
    Inactivation means that the points is moved last in the array.
    If no unflagged replicates exists, this function will insert the resp_best
    response as an unflagged replicate if available.
    ******************************************************************************/
    private static int DE_InactivateRepl(int point, tagSTDCURVE stdcurve, bool[] normdose_flg,//[METHOD_MAXSTDREPL],
                   bool[] deviation_flg,//[METHOD_MAXSTDREPL],
                   bool resp_best_flg, double resp_best, ref bool artificial_flg, bool skip_artificial_flg)
    {
        var no_OK = 0;
        var resp_OK = new double[METHOD_MAXSTDREPL];
        var no_NG = 0;
        var resp_NG = new double[METHOD_MAXSTDREPL];
        int repl;

        // Create array of unflagged and flagged replicates
        for (repl = 0; repl < stdcurve.ndataorg[point]; repl++)
        {
            if (normdose_flg[repl] == false && deviation_flg[repl] == false)
            {
                resp_OK[no_OK] = stdcurve.resporg[point, repl];
                no_OK++;
            }
            else
            {
                resp_NG[no_NG] = stdcurve.resporg[point, repl];
                no_NG++;
            }
        }

        // Insert unflagged replicates
        stdcurve.ndata[point] = 0;
        repl = 0;
        int i;
        if (no_OK > 0)
        {
            for (i = 0; i < no_OK && repl < METHOD_MAXSTDREPL; i++)
            {
                stdcurve.resporg[point, repl] = resp_OK[i];
                stdcurve.resp[point, repl] = resp_OK[i];
                repl++;
            }
            stdcurve.ndata[point] = no_OK;
        }
        // In case no unflagged replicate and calculated response exists, insert calculated response
        else if (resp_best_flg == true && skip_artificial_flg == false)
        {
            stdcurve.resporg[point, 0] = 0;
            stdcurve.resp[point, 0] = resp_best;
            stdcurve.ndata[point] = 1;
            artificial_flg = true;
            repl = 1;
        }

        // Insert flagged replicates
        if (no_NG > 0)
        {
            for (i = 0; i < no_NG && repl < METHOD_MAXSTDREPL; i++)
            {
                stdcurve.resporg[point, repl] = resp_NG[i];
                stdcurve.resp[point, repl] = resp_NG[i];
                repl++;
            }
        }

        stdcurve.ndataorg[point] = repl;

        return no_NG;
    }


    public static bool[] GetRow(bool[,] matrix, int rowNumber)
    {
        return Enumerable.Range(0, matrix.GetLength(1))
                .Select(x => matrix[rowNumber, x])
                .ToArray();
    }

    public static double[] GetRow(double[,] matrix, int rowNumber)
    {
        return Enumerable.Range(0, matrix.GetLength(1))
                .Select(x => matrix[rowNumber, x])
                .ToArray();
    }

    private static int Abs(int v)
    {
        return Math.Abs(v);
    }

    private static double Fabs(double v)
    {
        return Math.Abs(v);
    }

    private static double Log(double v)
    {
        return Math.Log(v);
    }

    private static double Sqrt(double s)
    {
        return Math.Sqrt(s);
    }
}

internal class tagSTDPOINT
{
    public double IdealResponse { get; set; }                            /* Ideal response                  */
    public double ExpectedSD { get; set; }                                /* Expected standard deviation     */
    public double UpperResponseLimit { get; set; }                       /* Upper limit for response        */
    public double LowerResponseLimit { get; set; }                       /* Lower limit for response        */
    public double LimitS_Precision { get; set; }                         /* Limit for precision (*expS)     */
    public double LimitS_Deviation { get; set; }                         /* Limit for deviation (*expS)     */
    public double UpperLogarithmicLimit { get; set; }                    /* Upper logarithmic limit         */
    public double LowerLogarithmicLimit { get; set; }                    /* Lower logarithmic limit         */
};

internal class tagMETHOD
{
    public tagMETHOD(int maxStandard)
    {
        std_point = new tagSTDPOINT[maxStandard];
    }

    public int std_maxbad { get; set; }                              /* Max number of bad replicates    */
    public int no_stdpoints { get; set; }                            /* No of Calibrator points         */
    public tagSTDPOINT[] std_point { get; set; }     /* Define Calibrator points        */

    public int calculation_zeropoint_switch { get; set; }            /* stdpoint where to switch (0=no zeropoint calc) */
    public int ExcludeLowestCalibratorFromMeanDiff { get; set; }     /* Set to 1 if lowest cal should be excluded from mean diff */
}

internal class tagSTDCURVE
{
    public tagSTDCURVE(int maxStandard, int maxStandardReplicateMethod)
    {
        ndataorg = new int[maxStandard];
        ndata = new int[maxStandard];
        resp = new double[maxStandard, maxStandardReplicateMethod];
        resporg = new double[maxStandard, maxStandardReplicateMethod];
        ndata_in = new int[maxStandard];
    }

    public int npoints { get; set; }                                     /* Number of standard points       */
    public int[] ndataorg { get; set; }                      /* Number of original replicates   */
    public int[] ndata { get; set; }                         /* Number of replicates            */
    public double[,] resp { get; set; }       /* Response data                   */
    public double[,] resporg { get; set; }    /* Response data                   */

    public bool shapeerror_CV { get; set; }                               /* defined in doc IM-LS 000127     */
    public bool shapeerror_flatcurve { get; set; }                        /* defined in doc IM-LS 000127     */
    public bool shapeerror_lowerparthigh { get; set; }                    /* defined in doc IM-LS 000127     */
    public bool shapeerror_singleabove { get; set; }                      /* defined in doc IM-LS 000127     */
    public bool shapeerror_singlebelow { get; set; }                      /* defined in doc IM-LS 000127     */
    public bool shapeerror_norisingtrend { get; set; }                    /* added 2002-11-21                */

    public int[] ndata_in { get; set; }                      /* Number of replicates sent in    */
}