﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dasz.LinqCube;

namespace dasz.LinqCube.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Building dimensions");
            var time = new Dimension<DateTime, Person>("Time", k => k.Birthday)
                    .BuildYear(1978, 2012)
                    .BuildMonths()
                    .Build<DateTime, Person>();

            var time_empstart = new Dimension<DateTime, Person>("Time employment start", k => k.EmploymentStart)
                    .BuildYearSlice(2001, 2011, 1, null, 9, null) // only look at jan-sept
                    .BuildMonths()
                    .Build<DateTime, Person>();

            var time_employment = new Dimension<DateTime, Person>("Time employment", k => k.EmploymentStart, k => k.EmploymentEnd ?? DateTime.MaxValue)
                    .BuildYear(2001, 2011)
                    .Build<DateTime, Person>();

            var gender = new Dimension<string, Person>("Gender", k => k.Gender)
                    .BuildEnum("M", "F")
                    .Build<string, Person>();

            var salary = new Dimension<decimal, Person>("Salary", k => k.Salary)
                    .BuildPartition(500, 1000, 2500)
                    .BuildPartition(100)
                    .Build<decimal, Person>();

            Dimension<string, Person> offices = new Dimension<string, Person>("Office", k => k.Office)
                .BuildEnum(Repository.OFFICES)
                .Build<string, Person>();

            Console.WriteLine("Building measures");
            var countAll = new CountMeasure<Person>("Count", k => true);

            var countEmployedFullMonth = new FilteredMeasure<Person, bool>("Count full month", k => k.EmploymentStart.Day == 1, countAll);

            var sumSalary = new DecimalSumMeasure<Person>("Sum of Salaries", k => k.Salary);

            Console.WriteLine("Building queries");
            var genderAgeQuery = new Query<Person>("gender over birthday")
                                    .WithDimension(time)
                                    .WithDimension(gender)
                                    .WithMeasure(countAll);

            var salaryQuery = new Query<Person>("salary over gender and date of employment")
                                    .WithDimension(time_empstart)
                                    .WithDimension(gender)
                                    .WithDimension(salary)
                                    .WithMeasure(countAll)
                                    .WithMeasure(countEmployedFullMonth)
                                    .WithMeasure(sumSalary);

            var countByOfficeQuery = new Query<Person>("count currently employed by office")
                                    .WithDimension(time_employment)
                                    .WithDimension(offices)
                                    .WithMeasure(countAll);

            CubeResult result;
            using (var ctx = new Repository())
            {
                result = Cube.Execute(ctx.Persons,
                                genderAgeQuery,
                                salaryQuery,
                                countByOfficeQuery
                );
            }

            ////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////

            Console.WriteLine(salaryQuery.Name);
            Console.WriteLine("==================");
            Console.WriteLine();
            foreach (var year in time_empstart.Children)
            {
                Console.WriteLine(year.Label);
                Console.WriteLine("==================");
                foreach (var gPart in salary.Children)
                {
                    foreach (var gPart2 in gPart.Children)
                    {
                        Console.WriteLine("{0}: {1,12}, M: {2,3} W: {3,3}, monthStart: {4,3}",
                            salary.Name,
                            gPart2.Label,
                            result[salaryQuery][year][gPart2][gender]["M"][countAll],
                            result[salaryQuery][year][gPart2][gender]["F"][countAll],
                            result[salaryQuery][year][gPart2][countEmployedFullMonth]
                            );
                    }
                }
                Console.WriteLine();
            }

            ////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////

            Console.WriteLine(countByOfficeQuery.Name);
            Console.WriteLine("==================");
            Console.WriteLine();
            Console.WriteLine("{0,10}|{1}",
                string.Empty,
                string.Join("|", time_employment.Children.Select(c => string.Format(" {0,6} ", c.Label)).ToArray())
            );
            Console.WriteLine("----------+--------+--------+--------+--------+--------+--------+--------+--------+--------+--------+--------");
            foreach (var officeEntry in offices.Children)
            {
                var officeCounts = result[countByOfficeQuery][officeEntry];
                Console.WriteLine("{0,10}|{1}",
                    officeEntry.Label,
                    string.Join("|", time_employment.Children.Select(c => string.Format(" {0,6} ", officeCounts[c][countAll])).ToArray())
                );
            }

            Console.WriteLine("Finished, hit the anykey to exit");
            Console.ReadKey();
        }
    }
}
