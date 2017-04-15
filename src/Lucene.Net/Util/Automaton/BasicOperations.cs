using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/*
 * dk.brics.automaton
 *
 * Copyright (c) 2001-2009 Anders Moeller
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * this SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * this SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Lucene.Net.Util.Automaton
{
    /// <summary>
    /// Basic automata operations.
    ///
    /// @lucene.experimental
    /// </summary>
    internal sealed class BasicOperations
    {
        private BasicOperations()
        {
        }

        /// <summary>
        /// Returns an automaton that accepts the concatenation of the languages of the
        /// given automata.
        /// <p>
        /// Complexity: linear in number of states.
        /// </summary>
        public static Automaton Concatenate(Automaton a1, Automaton a2)
        {
            if (a1.IsSingleton && a2.IsSingleton)
            {
                return BasicAutomata.MakeString(a1.singleton + a2.singleton);
            }
            if (IsEmpty(a1) || IsEmpty(a2))
            {
                return BasicAutomata.MakeEmpty();
            }
            // adding epsilon transitions with the NFA concatenation algorithm
            // in this case always produces a resulting DFA, preventing expensive
            // redundant determinize() calls for this common case.
            bool deterministic = a1.IsSingleton && a2.IsDeterministic;
            if (a1 == a2)
            {
                a1 = a1.CloneExpanded();
                a2 = a2.CloneExpanded();
            }
            else
            {
                a1 = a1.CloneExpandedIfRequired();
                a2 = a2.CloneExpandedIfRequired();
            }
            foreach (State s in a1.GetAcceptStates())
            {
                s.accept = false;
                s.AddEpsilon(a2.initial);
            }
            a1.deterministic = deterministic;
            //a1.clearHashCode();
            a1.ClearNumberedStates();
            a1.CheckMinimizeAlways();
            return a1;
        }

        /// <summary>
        /// Returns an automaton that accepts the concatenation of the languages of the
        /// given automata.
        /// <p>
        /// Complexity: linear in total number of states.
        /// </summary>
        public static Automaton Concatenate(IList<Automaton> l)
        {
            if (l.Count == 0)
            {
                return BasicAutomata.MakeEmptyString();
            }
            bool all_singleton = true;
            foreach (Automaton a in l)
            {
                if (!a.IsSingleton)
                {
                    all_singleton = false;
                    break;
                }
            }
            if (all_singleton)
            {
                StringBuilder b = new StringBuilder();
                foreach (Automaton a in l)
                {
                    b.Append(a.singleton);
                }
                return BasicAutomata.MakeString(b.ToString());
            }
            else
            {
                foreach (Automaton a in l)
                {
                    if (BasicOperations.IsEmpty(a))
                    {
                        return BasicAutomata.MakeEmpty();
                    }
                }
                HashSet<int> ids = new HashSet<int>();
                foreach (Automaton a in l)
                {
                    ids.Add(a.GetHashCode());
                }
                bool has_aliases = ids.Count != l.Count;
                Automaton b = l[0];
                if (has_aliases)
                {
                    b = b.CloneExpanded();
                }
                else
                {
                    b = b.CloneExpandedIfRequired();
                }
                ISet<State> ac = b.GetAcceptStates();
                bool first = true;
                foreach (Automaton a in l)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        if (a.IsEmptyString)
                        {
                            continue;
                        }
                        Automaton aa = a;
                        if (has_aliases)
                        {
                            aa = aa.CloneExpanded();
                        }
                        else
                        {
                            aa = aa.CloneExpandedIfRequired();
                        }
                        ISet<State> ns = aa.GetAcceptStates();
                        foreach (State s in ac)
                        {
                            s.accept = false;
                            s.AddEpsilon(aa.initial);
                            if (s.accept)
                            {
                                ns.Add(s);
                            }
                        }
                        ac = ns;
                    }
                }
                b.deterministic = false;
                //b.clearHashCode();
                b.ClearNumberedStates();
                b.CheckMinimizeAlways();
                return b;
            }
        }

        /// <summary>
        /// Returns an automaton that accepts the union of the empty string and the
        /// language of the given automaton.
        /// <p>
        /// Complexity: linear in number of states.
        /// </summary>
        public static Automaton Optional(Automaton a)
        {
            a = a.CloneExpandedIfRequired();
            State s = new State();
            s.AddEpsilon(a.initial);
            s.accept = true;
            a.initial = s;
            a.deterministic = false;
            //a.clearHashCode();
            a.ClearNumberedStates();
            a.CheckMinimizeAlways();
            return a;
        }

        /// <summary>
        /// Returns an automaton that accepts the Kleene star (zero or more
        /// concatenated repetitions) of the language of the given automaton. Never
        /// modifies the input automaton language.
        /// <p>
        /// Complexity: linear in number of states.
        /// </summary>
        public static Automaton Repeat(Automaton a)
        {
            a = a.CloneExpanded();
            State s = new State();
            s.accept = true;
            s.AddEpsilon(a.initial);
            foreach (State p in a.GetAcceptStates())
            {
                p.AddEpsilon(s);
            }
            a.initial = s;
            a.deterministic = false;
            //a.clearHashCode();
            a.ClearNumberedStates();
            a.CheckMinimizeAlways();
            return a;
        }

        /// <summary>
        /// Returns an automaton that accepts <code>min</code> or more concatenated
        /// repetitions of the language of the given automaton.
        /// <p>
        /// Complexity: linear in number of states and in <code>min</code>.
        /// </summary>
        public static Automaton Repeat(Automaton a, int min)
        {
            if (min == 0)
            {
                return Repeat(a);
            }
            IList<Automaton> @as = new List<Automaton>();
            while (min-- > 0)
            {
                @as.Add(a);
            }
            @as.Add(Repeat(a));
            return Concatenate(@as);
        }

        /// <summary>
        /// Returns an automaton that accepts between <code>min</code> and
        /// <code>max</code> (including both) concatenated repetitions of the language
        /// of the given automaton.
        /// <p>
        /// Complexity: linear in number of states and in <code>min</code> and
        /// <code>max</code>.
        /// </summary>
        public static Automaton Repeat(Automaton a, int min, int max)
        {
            if (min > max)
            {
                return BasicAutomata.MakeEmpty();
            }
            max -= min;
            a.ExpandSingleton();
            Automaton b;
            if (min == 0)
            {
                b = BasicAutomata.MakeEmptyString();
            }
            else if (min == 1)
            {
                b = (Automaton)a.Clone();
            }
            else
            {
                IList<Automaton> @as = new List<Automaton>();
                while (min-- > 0)
                {
                    @as.Add(a);
                }
                b = Concatenate(@as);
            }
            if (max > 0)
            {
                Automaton d = (Automaton)a.Clone();
                while (--max > 0)
                {
                    Automaton c = (Automaton)a.Clone();
                    foreach (State p in c.GetAcceptStates())
                    {
                        p.AddEpsilon(d.initial);
                    }
                    d = c;
                }
                foreach (State p in b.GetAcceptStates())
                {
                    p.AddEpsilon(d.initial);
                }
                b.deterministic = false;
                //b.clearHashCode();
                b.ClearNumberedStates();
                b.CheckMinimizeAlways();
            }
            return b;
        }

        /// <summary>
        /// Returns a (deterministic) automaton that accepts the complement of the
        /// language of the given automaton.
        /// <p>
        /// Complexity: linear in number of states (if already deterministic).
        /// </summary>
        public static Automaton Complement(Automaton a)
        {
            a = a.CloneExpandedIfRequired();
            a.Determinize();
            a.Totalize();
            foreach (State p in a.GetNumberedStates())
            {
                p.accept = !p.accept;
            }
            a.RemoveDeadTransitions();
            return a;
        }

        /// <summary>
        /// Returns a (deterministic) automaton that accepts the intersection of the
        /// language of <code>a1</code> and the complement of the language of
        /// <code>a2</code>. As a side-effect, the automata may be determinized, if not
        /// already deterministic.
        /// <p>
        /// Complexity: quadratic in number of states (if already deterministic).
        /// </summary>
        public static Automaton Minus(Automaton a1, Automaton a2)
        {
            if (BasicOperations.IsEmpty(a1) || a1 == a2)
            {
                return BasicAutomata.MakeEmpty();
            }
            if (BasicOperations.IsEmpty(a2))
            {
                return a1.CloneIfRequired();
            }
            if (a1.IsSingleton)
            {
                if (BasicOperations.Run(a2, a1.singleton))
                {
                    return BasicAutomata.MakeEmpty();
                }
                else
                {
                    return a1.CloneIfRequired();
                }
            }
            return Intersection(a1, a2.Complement());
        }

        /// <summary>
        /// Returns an automaton that accepts the intersection of the languages of the
        /// given automata. Never modifies the input automata languages.
        /// <p>
        /// Complexity: quadratic in number of states.
        /// </summary>
        public static Automaton Intersection(Automaton a1, Automaton a2)
        {
            if (a1.IsSingleton)
            {
                if (BasicOperations.Run(a2, a1.singleton))
                {
                    return a1.CloneIfRequired();
                }
                else
                {
                    return BasicAutomata.MakeEmpty();
                }
            }
            if (a2.IsSingleton)
            {
                if (BasicOperations.Run(a1, a2.singleton))
                {
                    return a2.CloneIfRequired();
                }
                else
                {
                    return BasicAutomata.MakeEmpty();
                }
            }
            if (a1 == a2)
            {
                return a1.CloneIfRequired();
            }
            Transition[][] transitions1 = a1.GetSortedTransitions();
            Transition[][] transitions2 = a2.GetSortedTransitions();
            Automaton c = new Automaton();
            LinkedList<StatePair> worklist = new LinkedList<StatePair>();
            Dictionary<StatePair, StatePair> newstates = new Dictionary<StatePair, StatePair>();
            StatePair p = new StatePair(c.initial, a1.initial, a2.initial);
            worklist.AddLast(p);
            newstates[p] = p;
            while (worklist.Count > 0)
            {
                p = worklist.First.Value;
                worklist.Remove(p);
                p.s.accept = p.S1.accept && p.S2.accept;
                Transition[] t1 = transitions1[p.S1.number];
                Transition[] t2 = transitions2[p.S2.number];
                for (int n1 = 0, b2 = 0; n1 < t1.Length; n1++)
                {
                    while (b2 < t2.Length && t2[b2].max < t1[n1].min)
                    {
                        b2++;
                    }
                    for (int n2 = b2; n2 < t2.Length && t1[n1].max >= t2[n2].min; n2++)
                    {
                        if (t2[n2].max >= t1[n1].min)
                        {
                            StatePair q = new StatePair(t1[n1].to, t2[n2].to);
                            StatePair r;
                            newstates.TryGetValue(q, out r);
                            if (r == null)
                            {
                                q.s = new State();
                                worklist.AddLast(q);
                                newstates[q] = q;
                                r = q;
                            }
                            int min = t1[n1].min > t2[n2].min ? t1[n1].min : t2[n2].min;
                            int max = t1[n1].max < t2[n2].max ? t1[n1].max : t2[n2].max;
                            p.s.AddTransition(new Transition(min, max, r.s));
                        }
                    }
                }
            }
            c.deterministic = a1.deterministic && a2.deterministic;
            c.RemoveDeadTransitions();
            c.CheckMinimizeAlways();
            return c;
        }

        /// <summary>
        /// Returns true if these two automata accept exactly the
        ///  same language.  this is a costly computation!  Note
        ///  also that a1 and a2 will be determinized as a side
        ///  effect.
        /// </summary>
        public static bool SameLanguage(Automaton a1, Automaton a2)
        {
            if (a1 == a2)
            {
                return true;
            }
            if (a1.IsSingleton && a2.IsSingleton)
            {
                return a1.singleton.Equals(a2.singleton, StringComparison.Ordinal);
            }
            else if (a1.IsSingleton)
            {
                // subsetOf is faster if the first automaton is a singleton
                return SubsetOf(a1, a2) && SubsetOf(a2, a1);
            }
            else
            {
                return SubsetOf(a2, a1) && SubsetOf(a1, a2);
            }
        }

        /// <summary>
        /// Returns true if the language of <code>a1</code> is a subset of the language
        /// of <code>a2</code>. As a side-effect, <code>a2</code> is determinized if
        /// not already marked as deterministic.
        /// <p>
        /// Complexity: quadratic in number of states.
        /// </summary>
        public static bool SubsetOf(Automaton a1, Automaton a2)
        {
            if (a1 == a2)
            {
                return true;
            }
            if (a1.IsSingleton)
            {
                if (a2.IsSingleton)
                {
                    return a1.singleton.Equals(a2.singleton, StringComparison.Ordinal);
                }
                return BasicOperations.Run(a2, a1.singleton);
            }
            a2.Determinize();
            Transition[][] transitions1 = a1.GetSortedTransitions();
            Transition[][] transitions2 = a2.GetSortedTransitions();
            LinkedList<StatePair> worklist = new LinkedList<StatePair>();
            HashSet<StatePair> visited = new HashSet<StatePair>();
            StatePair p = new StatePair(a1.initial, a2.initial);
            worklist.AddLast(p);
            visited.Add(p);
            while (worklist.Count > 0)
            {
                p = worklist.First.Value;
                worklist.Remove(p);
                if (p.S1.accept && !p.S2.accept)
                {
                    return false;
                }
                Transition[] t1 = transitions1[p.S1.number];
                Transition[] t2 = transitions2[p.S2.number];
                for (int n1 = 0, b2 = 0; n1 < t1.Length; n1++)
                {
                    while (b2 < t2.Length && t2[b2].max < t1[n1].min)
                    {
                        b2++;
                    }
                    int min1 = t1[n1].min, max1 = t1[n1].max;

                    for (int n2 = b2; n2 < t2.Length && t1[n1].max >= t2[n2].min; n2++)
                    {
                        if (t2[n2].min > min1)
                        {
                            return false;
                        }
                        if (t2[n2].max < Character.MAX_CODE_POINT)
                        {
                            min1 = t2[n2].max + 1;
                        }
                        else
                        {
                            min1 = Character.MAX_CODE_POINT;
                            max1 = Character.MIN_CODE_POINT;
                        }
                        StatePair q = new StatePair(t1[n1].to, t2[n2].to);
                        if (!visited.Contains(q))
                        {
                            worklist.AddLast(q);
                            visited.Add(q);
                        }
                    }
                    if (min1 <= max1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns an automaton that accepts the union of the languages of the given
        /// automata.
        /// <p>
        /// Complexity: linear in number of states.
        /// </summary>
        public static Automaton Union(Automaton a1, Automaton a2)
        {
            if ((a1.IsSingleton && a2.IsSingleton && a1.singleton.Equals(a2.singleton, StringComparison.Ordinal)) || a1 == a2)
            {
                return a1.CloneIfRequired();
            }
            if (a1 == a2)
            {
                a1 = a1.CloneExpanded();
                a2 = a2.CloneExpanded();
            }
            else
            {
                a1 = a1.CloneExpandedIfRequired();
                a2 = a2.CloneExpandedIfRequired();
            }
            State s = new State();
            s.AddEpsilon(a1.initial);
            s.AddEpsilon(a2.initial);
            a1.initial = s;
            a1.deterministic = false;
            //a1.clearHashCode();
            a1.ClearNumberedStates();
            a1.CheckMinimizeAlways();
            return a1;
        }

        /// <summary>
        /// Returns an automaton that accepts the union of the languages of the given
        /// automata.
        /// <p>
        /// Complexity: linear in number of states.
        /// </summary>
        public static Automaton Union(ICollection<Automaton> l)
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (Automaton a in l)
            {
                ids.Add(a.GetHashCode());
            }
            bool has_aliases = ids.Count != l.Count;
            State s = new State();
            foreach (Automaton b in l)
            {
                if (BasicOperations.IsEmpty(b))
                {
                    continue;
                }
                Automaton bb = b;
                if (has_aliases)
                {
                    bb = bb.CloneExpanded();
                }
                else
                {
                    bb = bb.CloneExpandedIfRequired();
                }
                s.AddEpsilon(bb.initial);
            }
            Automaton a_ = new Automaton();
            a_.initial = s;
            a_.deterministic = false;
            //a.clearHashCode();
            a_.ClearNumberedStates();
            a_.CheckMinimizeAlways();
            return a_;
        }

        // Simple custom ArrayList<Transition>
        private sealed class TransitionList
        {
            internal Transition[] transitions = new Transition[2];
            internal int count;

            public void Add(Transition t)
            {
                if (transitions.Length == count)
                {
                    Transition[] newArray = new Transition[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(transitions, 0, newArray, 0, count);
                    transitions = newArray;
                }
                transitions[count++] = t;
            }
        }

        // Holds all transitions that start on this int point, or
        // end at this point-1
        private sealed class PointTransitions : IComparable<PointTransitions>
        {
            internal int point;
            internal readonly TransitionList ends = new TransitionList();
            internal readonly TransitionList starts = new TransitionList();

            public int CompareTo(PointTransitions other)
            {
                return point - other.point;
            }

            public void Reset(int point)
            {
                this.point = point;
                ends.count = 0;
                starts.count = 0;
            }

            public override bool Equals(object other)
            {
                return ((PointTransitions)other).point == point;
            }

            public override int GetHashCode()
            {
                return point;
            }
        }

        private sealed class PointTransitionSet
        {
            internal int count;
            internal PointTransitions[] points = new PointTransitions[5];

            private const int HASHMAP_CUTOVER = 30;
            private readonly Dictionary<int?, PointTransitions> map = new Dictionary<int?, PointTransitions>();
            private bool useHash = false;

            private PointTransitions Next(int point)
            {
                // 1st time we are seeing this point
                if (count == points.Length)
                {
                    PointTransitions[] newArray = new PointTransitions[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(points, 0, newArray, 0, count);
                    points = newArray;
                }
                PointTransitions points0 = points[count];
                if (points0 == null)
                {
                    points0 = points[count] = new PointTransitions();
                }
                points0.Reset(point);
                count++;
                return points0;
            }

            private PointTransitions Find(int point)
            {
                if (useHash)
                {
                    int? pi = point;
                    PointTransitions p;
                    if (!map.TryGetValue(pi, out p))
                    {
                        p = Next(point);
                        map[pi] = p;
                    }
                    return p;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (points[i].point == point)
                        {
                            return points[i];
                        }
                    }

                    PointTransitions p = Next(point);
                    if (count == HASHMAP_CUTOVER)
                    {
                        // switch to HashMap on the fly
                        Debug.Assert(map.Count == 0);
                        for (int i = 0; i < count; i++)
                        {
                            map[points[i].point] = points[i];
                        }
                        useHash = true;
                    }
                    return p;
                }
            }

            public void Reset()
            {
                if (useHash)
                {
                    map.Clear();
                    useHash = false;
                }
                count = 0;
            }

            public void Sort()
            {
                // Tim sort performs well on already sorted arrays:
                if (count > 1)
                {
                    ArrayUtil.TimSort(points, 0, count);
                }
            }

            public void Add(Transition t)
            {
                Find(t.min).starts.Add(t);
                Find(1 + t.max).ends.Add(t);
            }

            public override string ToString()
            {
                StringBuilder s = new StringBuilder();
                for (int i = 0; i < count; i++)
                {
                    if (i > 0)
                    {
                        s.Append(' ');
                    }
                    s.Append(points[i].point).Append(':').Append(points[i].starts.count).Append(',').Append(points[i].ends.count);
                }
                return s.ToString();
            }
        }

        /// <summary>
        /// Determinizes the given automaton.
        /// <p>
        /// Worst case complexity: exponential in number of states.
        /// </summary>
        public static void Determinize(Automaton a)
        {
            if (a.IsDeterministic || a.IsSingleton)
            {
                return;
            }

            State[] allStates = a.GetNumberedStates();

            // subset construction
            bool initAccept = a.initial.accept;
            int initNumber = a.initial.number;
            a.initial = new State();
            SortedInt32Set.FrozenInt32Set initialset = new SortedInt32Set.FrozenInt32Set(initNumber, a.initial);

            LinkedList<SortedInt32Set.FrozenInt32Set> worklist = new LinkedList<SortedInt32Set.FrozenInt32Set>();
            IDictionary<SortedInt32Set.FrozenInt32Set, State> newstate = new Dictionary<SortedInt32Set.FrozenInt32Set, State>();

            worklist.AddLast(initialset);

            a.initial.accept = initAccept;
            newstate[initialset] = a.initial;

            int newStateUpto = 0;
            State[] newStatesArray = new State[5];
            newStatesArray[newStateUpto] = a.initial;
            a.initial.number = newStateUpto;
            newStateUpto++;

            // like Set<Integer,PointTransitions>
            PointTransitionSet points = new PointTransitionSet();

            // like SortedMap<Integer,Integer>
            SortedInt32Set statesSet = new SortedInt32Set(5);

            // LUCENENET TODO: THIS IS INFINITE LOOPING

            // LUCENENET NOTE: The problem here is almost certainly 
            // due to the conversion to FrozenIntSet along with its
            // differing equality checking.
            while (worklist.Count > 0)
            {
                SortedInt32Set.FrozenInt32Set s = worklist.First.Value;
                worklist.Remove(s);

                // Collate all outgoing transitions by min/1+max:
                for (int i = 0; i < s.values.Length; i++)
                {
                    State s0 = allStates[s.values[i]];
                    for (int j = 0; j < s0.numTransitions; j++)
                    {
                        points.Add(s0.TransitionsArray[j]);
                    }
                }

                if (points.count == 0)
                {
                    // No outgoing transitions -- skip it
                    continue;
                }

                points.Sort();

                int lastPoint = -1;
                int accCount = 0;

                State r = s.state;
                for (int i = 0; i < points.count; i++)
                {
                    int point = points.points[i].point;

                    if (statesSet.upto > 0)
                    {
                        Debug.Assert(lastPoint != -1);

                        statesSet.ComputeHash();

                        State q;
                        if (!newstate.TryGetValue(statesSet.ToFrozenInt32Set(), out q) || q == null)
                        {
                            q = new State();

                            SortedInt32Set.FrozenInt32Set p = statesSet.Freeze(q);
                            worklist.AddLast(p);
                            if (newStateUpto == newStatesArray.Length)
                            {
                                State[] newArray = new State[ArrayUtil.Oversize(1 + newStateUpto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                                Array.Copy(newStatesArray, 0, newArray, 0, newStateUpto);
                                newStatesArray = newArray;
                            }
                            newStatesArray[newStateUpto] = q;
                            q.number = newStateUpto;
                            newStateUpto++;
                            q.accept = accCount > 0;
                            newstate[p] = q;
                        }
                        else
                        {
                            Debug.Assert((accCount > 0) == q.accept, "accCount=" + accCount + " vs existing accept=" + q.accept + " states=" + statesSet);
                        }

                        r.AddTransition(new Transition(lastPoint, point - 1, q));
                    }

                    // process transitions that end on this point
                    // (closes an overlapping interval)
                    Transition[] transitions = points.points[i].ends.transitions;
                    int limit = points.points[i].ends.count;
                    for (int j = 0; j < limit; j++)
                    {
                        Transition t = transitions[j];
                        int num = t.to.number;
                        statesSet.Decr(num);
                        accCount -= t.to.accept ? 1 : 0;
                    }
                    points.points[i].ends.count = 0;

                    // process transitions that start on this point
                    // (opens a new interval)
                    transitions = points.points[i].starts.transitions;
                    limit = points.points[i].starts.count;
                    for (int j = 0; j < limit; j++)
                    {
                        Transition t = transitions[j];
                        int num = t.to.number;
                        statesSet.Incr(num);
                        accCount += t.to.accept ? 1 : 0;
                    }
                    lastPoint = point;
                    points.points[i].starts.count = 0;
                }
                points.Reset();
                Debug.Assert(statesSet.upto == 0, "upto=" + statesSet.upto);
            }
            a.deterministic = true;
            a.SetNumberedStates(newStatesArray, newStateUpto);
        }

        /// <summary>
        /// Adds epsilon transitions to the given automaton. this method adds extra
        /// character interval transitions that are equivalent to the given set of
        /// epsilon transitions.
        /// </summary>
        /// <param name="pairs"> collection of <seealso cref="StatePair"/> objects representing pairs of
        ///          source/destination states where epsilon transitions should be
        ///          added </param>
        public static void AddEpsilons(Automaton a, ICollection<StatePair> pairs)
        {
            a.ExpandSingleton();
            Dictionary<State, HashSet<State>> forward = new Dictionary<State, HashSet<State>>();
            Dictionary<State, HashSet<State>> back = new Dictionary<State, HashSet<State>>();
            foreach (StatePair p in pairs)
            {
                HashSet<State> to;
                if (!forward.TryGetValue(p.S1, out to))
                {
                    to = new HashSet<State>();
                    forward[p.S1] = to;
                }
                to.Add(p.S2);
                HashSet<State> from;
                if (!back.TryGetValue(p.S2, out from))
                {
                    from = new HashSet<State>();
                    back[p.S2] = from;
                }
                from.Add(p.S1);
            }
            // calculate epsilon closure
            LinkedList<StatePair> worklist = new LinkedList<StatePair>(pairs);
            HashSet<StatePair> workset = new HashSet<StatePair>(pairs);
            while (worklist.Count > 0)
            {
                StatePair p = worklist.First.Value;
                worklist.Remove(p);
                workset.Remove(p);
                HashSet<State> to;
                HashSet<State> from;
                if (forward.TryGetValue(p.S2, out to))
                {
                    foreach (State s in to)
                    {
                        StatePair pp = new StatePair(p.S1, s);
                        if (!pairs.Contains(pp))
                        {
                            pairs.Add(pp);
                            forward[p.S1].Add(s);
                            back[s].Add(p.S1);
                            worklist.AddLast(pp);
                            workset.Add(pp);
                            if (back.TryGetValue(p.S1, out from))
                            {
                                foreach (State q in from)
                                {
                                    StatePair qq = new StatePair(q, p.S1);
                                    if (!workset.Contains(qq))
                                    {
                                        worklist.AddLast(qq);
                                        workset.Add(qq);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // add transitions
            foreach (StatePair p in pairs)
            {
                p.S1.AddEpsilon(p.S2);
            }
            a.deterministic = false;
            //a.clearHashCode();
            a.ClearNumberedStates();
            a.CheckMinimizeAlways();
        }

        /// <summary>
        /// Returns true if the given automaton accepts the empty string and nothing
        /// else.
        /// </summary>
        public static bool IsEmptyString(Automaton a)
        {
            if (a.IsSingleton)
            {
                return a.singleton.Length == 0;
            }
            else
            {
                return a.initial.accept && a.initial.NumTransitions == 0;
            }
        }

        /// <summary>
        /// Returns true if the given automaton accepts no strings.
        /// </summary>
        public static bool IsEmpty(Automaton a)
        {
            if (a.IsSingleton)
            {
                return false;
            }
            return !a.initial.accept && a.initial.NumTransitions == 0;
        }

        /// <summary>
        /// Returns true if the given automaton accepts all strings.
        /// </summary>
        public static bool IsTotal(Automaton a)
        {
            if (a.IsSingleton)
            {
                return false;
            }
            if (a.initial.accept && a.initial.NumTransitions == 1)
            {
                var iter = a.initial.GetTransitions().GetEnumerator();
                iter.MoveNext();
                Transition t = iter.Current; ;
                return t.to == a.initial && t.min == Character.MIN_CODE_POINT && t.max == Character.MAX_CODE_POINT;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the given string is accepted by the automaton.
        /// <p>
        /// Complexity: linear in the length of the string.
        /// <p>
        /// <b>Note:</b> for full performance, use the <seealso cref="RunAutomaton"/> class.
        /// </summary>
        public static bool Run(Automaton a, string s)
        {
            if (a.IsSingleton)
            {
                return s.Equals(a.singleton, StringComparison.Ordinal);
            }
            if (a.deterministic)
            {
                State p = a.initial;
                for (int i = 0, cp = 0; i < s.Length; i += Character.CharCount(cp))
                {
                    State q = p.Step(cp = Character.CodePointAt(s, i));
                    if (q == null)
                    {
                        return false;
                    }
                    p = q;
                }
                return p.accept;
            }
            else
            {
                State[] states = a.GetNumberedStates();
                LinkedList<State> pp = new LinkedList<State>();
                LinkedList<State> pp_other = new LinkedList<State>();
                OpenBitSet bb = new OpenBitSet(states.Length);
                OpenBitSet bb_other = new OpenBitSet(states.Length);
                pp.AddLast(a.initial);
                List<State> dest = new List<State>();
                bool accept = a.initial.accept;
                for (int i = 0, c = 0; i < s.Length; i += Character.CharCount(c))
                {
                    c = Character.CodePointAt(s, i);
                    accept = false;
                    pp_other.Clear();
                    bb_other.Clear(0, bb_other.Length - 1);
                    foreach (State p in pp)
                    {
                        dest.Clear();
                        p.Step(c, dest);
                        foreach (State q in dest)
                        {
                            if (q.accept)
                            {
                                accept = true;
                            }
                            if (!bb_other.Get(q.number))
                            {
                                bb_other.Set(q.number);
                                pp_other.AddLast(q);
                            }
                        }
                    }
                    LinkedList<State> tp = pp;
                    pp = pp_other;
                    pp_other = tp;
                    OpenBitSet tb = bb;
                    bb = bb_other;
                    bb_other = tb;
                }
                return accept;
            }
        }
    }
}