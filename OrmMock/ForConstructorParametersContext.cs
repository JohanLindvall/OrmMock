// Copyright(c) 2017, 2018 Johan Lindvall
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace OrmMock
{
    /// <summary>
    /// Implements creation options for a type when used as a constructor parameter.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    public class ForConstructorParametersContext<T> : ICreationsOptions<T>
    {
        /// <summary>
        /// Holds the type context.
        /// </summary>
        private readonly ForTypeContext<T> typeContext;

        /// <summary>
        /// Holds the structure.
        /// </summary>
        private readonly Structure structure;

        /// <summary>
        /// Initializes a new instance of the ForConstructorParametersContext class.
        /// </summary>
        /// <param name="typeContext">The type context.</param>
        /// <param name="structure">The structure.</param>
        public ForConstructorParametersContext(ForTypeContext<T> typeContext, Structure structure)
        {
            this.typeContext = typeContext;
            this.structure = structure;
        }

        /// <inheritdoc />
        public ForTypeContext<T> Skip() => this.SetCreationOption(CreationOptions.Skip);

        /// <inheritdoc />
        public ForTypeContext<T> IgnoreParents() => this.SetCreationOption(CreationOptions.IgnoreInheritance);

        /// <inheritdoc />
        public ForTypeContext<T> OnlyDirectParent() => this.SetCreationOption(CreationOptions.OnlyDirectInheritance);

        /// <inheritdoc />
        public ForTypeContext<T> OnlyParents() => this.SetCreationOption(CreationOptions.OnlyInheritance);

        private ForTypeContext<T> SetCreationOption(CreationOptions value)
        {
            this.structure.ConstructorCustomization[typeof(T)] = value;

            return this.typeContext;
        }
    }
}
