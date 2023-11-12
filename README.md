# How about making a program doing fluid simulations?
<img align = "right" src = "https://github.com/jensmalmgren/fluid-simulations-in-csharp/assets/20211468/0db24f7b-7bb0-48b1-813d-595dee767d34">As the title says, would it not be cool to reproduce fluid simulations in your computer program? I am not good at math, so I started googling for examples of how to do this. I knew it had to run on c# because I wanted the simulation integrated into an existing c# program.

I found Mattias Muller at Ten Minute Physics; he knows about math. He made a [YouTube movie](https://www.youtube.com/watch?v=iKAVRgIrUOU) about the subject and published the source code on GitHub, so from that perspective, I had all the ingredients for porting his code into c#. He wrote his code in JavaScript. He made a big deal out of being able to produce the simulation from just 200 lines of code. In my situation, that was absolutely nothing I cared about; besides that, I put begin curly braces on their own line of code to make the code more readable, so I am losing the line count battle anyway.

The program of Mattias stores two-dimensional values in one-dimensional arrays. That is common practice in fluid simulation programs. However, he is not mapping from 2d to 1d via a function, making the indexation into the arrays hard to read. The next challenge is the JavaScript language because it is not typed. Mattias has not indicated the intended type in the names of variables, so when porting the code, I had to single-step his program and look at the values to deduce the types. This is a magnificent approach until the program crosses integer and floating-type data. I broke my back on this challenge and had not even started understanding the math.

Another challenge with fluid simulation, which is not only a problem with Mattias source code, is that we are easily talking about hundreds of values being manipulated in fluid simulation programs. You quickly get lost in the data.

I have put a considerable amount of effort into porting Mattias program. He bolted on extra features, scenes, obstacles, etc, making the program more exciting but harder for the beginner to understand. JavaScript has a messy type system. So I abandoned Mattias source code, clueless and disappointed.

Next up, I found The Coding Train. This was a [long video](https://www.youtube.com/watch?v=alhpH6ECFvQ) by Daniel porting a C++/C program into Java. Look, now I was excited because going from one typed language to another typed language is just blissfully simple. The original source code from Mike Ash was a 3D simulation, while Daniel, on the fly, made it into a 2D simulation. Seeing Daniel do it was exhausting but promising. He said he was going to publish this source code, but I never found that source code. So, what do I do? I also did what Daniel did, porting Mike Ash's source code from C++/C into c#, making it 2D. Did it work? Kind of. Daniel is clear that he did not understand the math of this all. Fine.

In the [thesis](https://mikeash.com/pyblog/fluid-simulation-for-dummies.html) of Mike Ash, he also points out that he does not understand all the math of it all. One good thing was that a function was used to map from 2D/3D into 1D. I like that. There were other things that I did not like. The boundary tests are +/-2 cells from the edge. I would prefer that all cell arithmetic is +/- 1 at most. However, the size of the grid is N.

What really put me off with Mike Ash's source code was that the function diffuse had argument x and x0, but he called it with x0 and x, and there I lost it; I was totally clueless. I could not follow the reasoning of it or repair the mistake; I was stuck. The code became useless to me. I broke my back on trying to find out what this was about.

I wanted more. I wanted to get to the core. And in this case, it turned out that Mike Ash got his inspiration from Jos Stam and [this paper](https://www.dgp.toronto.edu/public_user/stam/reality/Research/pdf/GDC03.pdf).

I decided to rework the Mike Ash source code and bring it back to Jos Stam's level. Jos uses an N + 2 grid, but the cell arithmetic is +/- one, as I want it to be. That is simpler, thus easier for the novice mathematician.
What are my additions to the Jos Stam program? Neither Mike Ash nor Jos Stam has used global variables but is passing non-changing variables as arguments. That is not efficient; it costs CPU time. Daniel was also thinking in this direction. I introduced a static class with all global variables. No more passing of things that don't change.

I introduced a cell boundary enumerable. Now you can see what the boundary test is about and not just a number 0, 1, 2. I changed the source to rectangular simulation areas, not just square ones.

Curiously, Jos Stam talks about viscosity, which is also represented in the source code but is not doing anything. That is okay. I am okay with that.

I introduced a flow similar to how Daniel from the CodeTrain did it. I also found that for my settings, the diffuse denominator of 1+4a made the system go out of hand. For me, 1+6a worked better.
When changing the grid size, you influence the workings of the diffusion, and I'm not too fond of that in Jos Stam's code, but I can live with that.

As I said, there are thousands of values, and it is challenging to only understand what is going on by looking at floating-point numbers. So, if you have a system that works, you can start playing around with it and learn more about it. So that is the meaning of this program. 350+ lines of code, which is not bad.

Here is a beautiful and informative video about fluid simulations explaining the math behind it all, and especially the math behind Jos Stams version of fluid simulations: https://www.youtube.com/watch?v=qsYE1wMEMPA
