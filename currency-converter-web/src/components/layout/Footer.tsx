import React from 'react';

export const Footer: React.FC = () => {
  return (
    <footer className="bg-gray-100 border-t border-gray-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
        <p className="text-center text-sm text-gray-600">
          © {new Date().getFullYear()} Currency Converter Demo. Data provided
          by Frankfurter API.
        </p>
      </div>
    </footer>
  );
};
